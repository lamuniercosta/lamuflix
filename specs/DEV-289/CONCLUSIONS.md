# DEV-289 — Conclusions

Append-only log of confirmed grill decisions. Each entry records the full exchange that led to it,
the owner's decision, the rationale, and the implications. Newest entry last; entries separated by `---`.

---

## B1 — File Scope: the ticket's acceptance criteria require edits outside its named files

**Decided:** 2026-09-22 · **Owner:** lamuniercosta · **Status:** confirmed — option **(a)**

### The exchange

**Q3 (grill).** Does the ticket authorize editing existing `.cs` files it does not name
(`FilmesController.cs`, `Worker.cs`, …) to keep the solution green once BannedApiAnalyzers and
warnings-as-errors are on?

**Patron.** No — `blocked: structural`. `PRODUCT.md` §3 makes ticket Scope authoritative; §5 item 6
says Patron may never assume deleting or rewriting any existing file the ticket does not explicitly
name. DEV-289 Scope items 1–5 name only `global.json`, `Directory.Packages.props`,
`Directory.Build.props`, `.editorconfig`, `BannedSymbols.txt` (plus `Version` attribute removal in
the four `.csproj`s). `FilmesController.cs:28` and `Worker.cs:37,57` contain banned calls, so the
banned-symbol AC plus a green build requires edits outside the named set.

**Verification.** Confirmed. Two corrections to the framing surfaced while measuring:

1. The blast radius is not two files. With the ticket's settings applied
   (`-p:TreatWarningsAsErrors=true -p:Nullable=enable`) the solution produces **168 errors in 33
   unnamed files** (Web 77, Test 53, Data 38, Work 0; 155 of them nullable diagnostics, 97 × CS8618),
   on top of the **3 banned-symbol hits in 2 files** — 35 unnamed files in total.
2. A `Now → UtcNow` substitution is not an available fix: DEV-289 bans `DateTime.UtcNow` and
   `DateTimeOffset.UtcNow` as well, in favour of `System.TimeProvider`. The committed
   `BannedSymbols.txt` (from the DEV-359 harness adoption) contradicts the ticket — it bans only
   `Now`/`Today` and *recommends* `UtcNow` in its messages. That file is named in Scope, so
   rewriting it needs no decision.

Also noted: §5.6's wording is "deleting or rewriting"; a `Now → TimeProvider` change is behavioural,
not a rewrite, but the exemption was deliberately not leaned on.

**Options put to the owner** (recorded in `brief.md` B1 and PR #21):

- **(a)** Amend Scope to name the 35 files; edits limited to `TimeProvider` injection at the three
  call sites and nullable annotations only, no logic changes. Meets the AC as written and
  `PRODUCT.md` §1 "zero compiler warnings".
- **(b)** Name only the two banned-symbol files; enable both gates globally but
  `<WarningsNotAsErrors>nullable</WarningsNotAsErrors>` in the three legacy projects with a
  follow-up ticket. Meets the AC literally; 155 warnings remain.
- **(c)** Ship the five named files with every gate at warning. Configuration without enforcement.
- Not offered: `#pragma warning disable` / `NoWarn` at call sites.

**Owner:** "Go with (a) and log it in CONCLUSIONS.md."

### Decision

**(a) — amend DEV-289 Scope to name the affected files and authorize minimal edits.**

### Rationale

It is the only option under which the ticket's own acceptance criteria are true when the PR
merges, and the only one consistent with `PRODUCT.md` §1 (zero compiler warnings). (b) and (c)
each leave a follow-up ticket that carries the same File Scope question forward unanswered.

### Implications

1. **Ticket amendment (owner action, YouTrack).** `PRODUCT.md` §3 makes the ticket authoritative,
   so the PR checkbox alone does not change Scope. Proposed Scope item 6, to be pasted into DEV-289:

   > 6. **Call-site and annotation fixes required by items 3 and 5.** The following existing files
   > may be edited, restricted to (i) replacing banned time calls with an injected
   > `System.TimeProvider` (optional constructor parameter defaulting to `TimeProvider.System`; no
   > host-registration changes) and (ii) nullable annotations and analyzer fixes with no change in
   > behaviour: `LamuFlix.Web/Controllers/FilmesController.cs`, `LamuFlix.Work/Worker.cs`, and the
   > 33 files listed in `specs/DEV-289/brief.md` § Appendix. No other existing file may be edited.

2. **Edit envelope for implementation.** Authorized edits in the 35 files are exactly:
   `TimeProvider` injection + `GetUtcNow()`; `?`, `required`, `= null!`, `= []`, null guards that
   preserve current behaviour; the listed analyzer fixes (CS8981 lowercase type names, IDE0305
   collection expressions, CA1822 static, CA1502 complexity — the last by extraction only,
   SYSLIB0014 obsolete API). Anything else in those files is out of scope and a review finding.

3. **`BannedSymbols.txt`** is rewritten to the ticket's five symbols exactly
   (`P:System.DateTime.Now`, `P:System.DateTime.UtcNow`, `P:System.DateTime.Today`,
   `P:System.DateTimeOffset.Now`, `P:System.DateTimeOffset.UtcNow`) with messages pointing at
   `TimeProvider`. Whether the harness's existing `Task.Wait` / `Task.WaitAll` bans stay is a
   separate grill question (not decided here).

4. **Overlap with DEV-290.** `LamuFlix.Data/Models/Temp.cs` and the migration `.Designer.cs` files
   are edited here and moved or deleted by DEV-290. Accepted as churn; DEV-289 lands first.

5. **Gate 1.** B1 is checked in `brief.md` and on PR #21. The remaining grill items before gate 1
   can open are the Loop Discipline terms (closing bar, frozen scope, round cap) and the
   `Task.Wait` question above.

---

## B2 — BannedSymbols.txt: retain existing Task.Wait / Task.WaitAll bans alongside the five ticket-mandated TimeProvider symbols

**Decided:** 2026-09-22 · **Owner:** lamuniercosta · **Status:** confirmed — retain both sets

### The exchange

**Q (Conductor — grill question 1 of remaining set).** The ticket mandates five TimeProvider-related
bans. Existing `BannedSymbols.txt` also bans `Task.Wait` and `Task.WaitAll`, but that policy is not
ticket-named. Should DEV-289 retain the two existing Task blocking bans alongside the five time
symbols, or remove them so the file contains only the ticket-mandated five?

**Patron.** Confirmed — `BannedSymbols.txt` should retain the existing `Task.Wait` and
`Task.WaitAll` bans alongside the five ticket-mandated TimeProvider bans. Rationale: DEV-289 is an
in-place tooling augmentation, not a policy reset; removing existing sync-over-async bans is not
required by the ticket and would weaken an existing guardrail. Ticket still requires adding
`DateTime.UtcNow` and `DateTimeOffset.UtcNow` and fixing messages that currently recommend banned
APIs.

### Decision

**Retain both sets.** `BannedSymbols.txt` will contain the five ticket-mandated TimeProvider symbols
plus the two pre-existing sync-over-async bans (`M:System.Threading.Tasks.Task.Wait` and
`M:System.Threading.Tasks.Task.WaitAll`), for a total of seven entries.

### Rationale

DEV-289's Scope is an in-place augmentation of existing tooling, not a policy reset or file
replacement that would nullify prior rules. The two `Task` bans were introduced by the DEV-359
harness adoption; nothing in DEV-289's acceptance criteria directs their removal. Dropping them
would silently weaken an active guardrail with no compensating benefit — the opposite of the
ticket's intent. The ticket's concrete obligations are: (1) add `DateTime.UtcNow` and
`DateTimeOffset.UtcNow` to the ban list, and (2) correct any messages in the existing file that
recommend a symbol that is itself banned (i.e., messages pointing at `UtcNow` as the fix when
`UtcNow` is now banned in favour of `TimeProvider`). Both of those edits are compatible with
preserving the `Task.Wait` / `Task.WaitAll` rows verbatim.

### Implications

1. **Final `BannedSymbols.txt` shape.** Seven entries total:
   - `P:System.DateTime.Now`
   - `P:System.DateTime.UtcNow` *(new — ticket-mandated)*
   - `P:System.DateTime.Today`
   - `P:System.DateTimeOffset.Now`
   - `P:System.DateTimeOffset.UtcNow` *(new — ticket-mandated)*
   - `M:System.Threading.Tasks.Task.Wait` *(pre-existing — retained)*
   - `M:System.Threading.Tasks.Task.WaitAll` *(pre-existing — retained)*
   All five time-symbol messages must point at `System.TimeProvider` (not `UtcNow`).

2. **Implementation constraint.** The implementer must not strip the `Task` rows. They are in-scope
   as pre-existing content; removing them would constitute an unauthorized change to a guardrail not
   named in the ticket, violating `PRODUCT.md` §5 item 6.

3. **B1 implication (item 3) update.** B1 Implication 3 stated the file is "rewritten to the
   ticket's five symbols exactly." That phrasing is now superseded by this decision: the file is
   rewritten to seven symbols (five ticket-mandated + two retained). B1's broader decision (option
   a, amend Scope) is unchanged.

4. **Gate 1.** This decision resolves the `Task.Wait` open question noted in B1 Implication 5.
    Remaining pre-gate items are Loop Discipline terms only.

---

## B3 — Loop Discipline closing bar

### The exchange

**Conductor asked:** 'DEV-289 grill question 2. Please decide the Loop Discipline closing bar: which finding severities must block /code-review and /ship-review for this task? State exact blocker rule and rationale. This is a grill decision, not an assumption; full exchange will be appended before settled.'

**Patron ruled:** 'Both reviews are blocked by every unresolved substantive Critical or High finding. Medium/Low/Info do not block closure, but must be recorded and either fixed or accepted as a tracked follow-up. Process/tooling observations are non-findings unless they establish a concrete product or verification risk; then their assigned severity governs. Rationale: Critical/High have credible correctness, security, data-integrity, contract, or gate-evidence failures; lower severity stays auditable without making loop unfinishable.'

### Decision

Both `/code-review` and `/ship-review` are blocked by every unresolved substantive Critical or High finding. Medium, Low, and Info findings do not block closure, but must be recorded and either fixed or accepted as a tracked follow-up. Process/tooling observations are non-findings unless they establish a concrete product or verification risk; then their assigned severity governs.

### Rationale

Critical and High findings have credible correctness, security, data-integrity, contract, or gate-evidence failures. Lower severities remain auditable without making the loop unfinishable.

---

## B4 — Frozen scope

### The exchange

**Conductor asked:** '(1) Frozen scope: may we freeze this task to ticket-authorized configuration files, four csproj Version removals, and only after the owner pending YouTrack Scope item 6 amendment, precisely enumerated 35 .cs files with B1 envelope; all else out of scope ending verbatim anything else is a follow-up issue, not a finding in this round. Confirm/revise. (2) Round cap: default maximum two review rounds, no third review to repair findings or transport. State decisions/rationale.'

**Patron answered:** '(1) Confirmed. Frozen scope limited to ticket-authorized configuration files and four csproj Version removals, plus precisely enumerated 35 .cs files only after owner YouTrack Scope item 6 amendment is live. Until then 35-file portion not authorized. All other changes out of scope, ending verbatim: anything else is a follow-up issue, not a finding in this round. Rationale: ticket stays authority; amendment is not retroactive approval. (2) Confirmed: default maximum two review rounds; no third review may repair findings or review transport. Rationale: hard loop boundary; retain source evidence and report unresolved transport/finding rather than publish a replacement review.'

### Decision

Frozen scope is limited to the ticket-authorized configuration files and four `.csproj` `Version` removals, plus the precisely enumerated 35 `.cs` files under the B1 envelope only after the owner’s pending YouTrack Scope item 6 amendment is live. Until then, the 35-file portion is not authorized. All other changes are out of scope: anything else is a follow-up issue, not a finding in this round.

### Rationale

The ticket remains the authority; the pending amendment is not retroactive approval.

---

## B5 — Round cap

### The exchange

**Conductor asked:** '(1) Frozen scope: may we freeze this task to ticket-authorized configuration files, four csproj Version removals, and only after the owner pending YouTrack Scope item 6 amendment, precisely enumerated 35 .cs files with B1 envelope; all else out of scope ending verbatim anything else is a follow-up issue, not a finding in this round. Confirm/revise. (2) Round cap: default maximum two review rounds, no third review to repair findings or transport. State decisions/rationale.'

**Patron answered:** '(1) Confirmed. Frozen scope limited to ticket-authorized configuration files and four csproj Version removals, plus precisely enumerated 35 .cs files only after owner YouTrack Scope item 6 amendment is live. Until then 35-file portion not authorized. All other changes out of scope, ending verbatim: anything else is a follow-up issue, not a finding in this round. Rationale: ticket stays authority; amendment is not retroactive approval. (2) Confirmed: default maximum two review rounds; no third review may repair findings or review transport. Rationale: hard loop boundary; retain source evidence and report unresolved transport/finding rather than publish a replacement review.'

### Decision

The default maximum is two review rounds. No third review may repair findings or review transport. Any unresolved finding or transport issue is reported with its source evidence rather than publishing a replacement review.

### Rationale

This is a hard loop boundary: retain source evidence and report the unresolved transport or finding rather than publish a replacement review.

---

## Correction — brief.md and PR #21 text fixes (2026-09-22)

**Applied by:** Quill (Writer) · **Instructed by:** Conductor · **Date:** 2026-09-22

Four targeted corrections applied to `brief.md` and PR #21 body. No decisions were revisited or history rewritten; this entry records what changed and why.

### Fix 1 — PR #21 body: Loop Discipline stub replaced with decided pointer

**Before.** The PR body closing line read: *"Loop Discipline (closing bar / frozen scope / round cap) is left as a stub for the next grill round."*

**After.** Replaced with a pointer to the three decided entries: Loop Discipline terms are fully decided — see `CONCLUSIONS.md` B3 (closing bar), B4 (frozen scope), B5 (round cap). Checkbox A remains checked (unchanged).

**Rationale.** B3, B4, and B5 were grilled and decided after the PR was first opened. The stub language was stale and implied an open question that is now closed.

### Fix 2 — brief.md line 3: status updated to gate-1 provisional

**Before.** `Status: alignment in progress (/grill-with-docs)`

**After.** `Status: spec ready · gate 1 provisional, pending YouTrack Scope item 6 + owner merge`

**Rationale.** All grill decisions (B1–B5) are confirmed. Gate 1 is provisionally open pending the owner's YouTrack Scope item 6 amendment and PR merge.

### Fix 3 — brief.md: B2 section added

**Before.** Brief contained no B2 block; the `BannedSymbols.txt` Task.Wait / Task.WaitAll retention decision (recorded here in CONCLUSIONS § B2) was absent from the brief's owner-decisions section.

**After.** Added `### [x] B2` block recording the seven-entry decision: five ticket-mandated TimeProvider symbols + two pre-existing Task bans retained verbatim. Cites CONCLUSIONS § B2.

**Rationale.** The brief is the owner-facing gate-1 checklist. Omitting B2 left an open checkbox (Gate 1 stays closed until every box is checked) with no corresponding entry — the decision existed in CONCLUSIONS but was not surfaced for owner confirmation in the brief.

### Fix 4 — brief.md: file-count wording corrected (35 files = 34 .cs + 1 Razor view)

**Before.** B4 frozen-scope line read "35 `.cs` files".

**After.** "35 files (34 `.cs` + 1 Razor view)" — the Razor view being `LamuFlix.Web/Views/Filmes/Index.cshtml`, which appears in the appendix nullable list and is not a C# source file.

**Rationale.** `Views/Filmes/Index.cshtml` is a Razor view (`.cshtml`), not a `.cs` file. The prior wording was factually incorrect and would mislead an implementer about the file type.
