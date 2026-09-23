# DEV-289 — Plan-Challenge Adjudication

[from Keel] · **Date:** 2026-09-22 · **Adjudicator:** Keel (Thinker) · **Stage:** 2 (plan challenge, Phase A)

**Base:** `claude/ticket-scope-cs-edits-3ed088` (from `origin/main` @ `aa02408`)
**Inputs:** `spec.md`, `plan.md`, `tasks.md`, `analyze.md`, `brief.md`, `CONCLUSIONS.md` (B1–B5);
`maestri note read findings-DEV-289-Ledger`; the Sentry (Risk) and Compass (Spec) plan-challenge findings
carried in the Conductor brief.
**Artifacts updated:** `spec.md`, `plan.md`, `tasks.md`, `analyze.md`, `checklists/requirements.md`, and this
file. `brief.md` and `CONCLUSIONS.md` are **preserved byte-for-byte** (append-only decision record).

**Method:** each finding was verified at its cited `file:line` against the worktree, then disposed under the
§2.3 blocked list, `PRODUCT.md` §5, B3 closing bar, and B4 frozen scope. A finding is accepted only when
the evidence reproduces; the disposition and the resulting edit are recorded per finding. **No production
code, ticket, PR, commit, or push was performed.**

---

## Hard boundary retained (all dispositions preserve it)

**Until the owner confirms Scope item 6 is live in YouTrack, all 35-file edits are unauthorized and
implementation may not begin.** This adjudication does not widen scope; it narrows and hardens it. T002 is
now a **hard prerequisite** for enforcement as well as remediation, and the branch must not be pushed or
merged into a red tree. `Gate 1` stays closed.

---

## HIGH

### H1 — SDK pin underspecified (Ledger) — **ACCEPT**

**Evidence.** `tasks.md` T003 and `spec.md` FR-001 said "pinning the SDK to the `10.0.x` band". `sdk.version`
requires a full version; `10.0.x` is not valid there, while any arbitrary patch defeats the reproducibility
goal. Owner decision D4(a) had already fixed the value: `10.0.400`, `rollForward: latestPatch`,
`allowPrerelease: false` (installed bands 10.0.201/302/400; `latestPatch` stays in-band, so `10.0.100` would
fail resolution). Verified: `dotnet --list-sdks` shows `10.0.400`.

**Disposition.** Accepted. Named the exact numeric SDK in every artifact; a wildcard is now explicitly
forbidden.
**Edits.** `spec.md` FR-001 / US1 independent test / acceptance scenario 1; `plan.md` Technical Context,
project-structure block, file-impact table; `tasks.md` T003.

### H2 — BannedApiAnalyzers has no pinned version (Ledger) — **ACCEPT**

**Evidence.** T012 required a centrally managed version but `plan.md` and `tasks.md` never stated it, and it
is absent from the inventory, so an implementer would either hit a CPM `NU1010`-class failure or invent a
version. Owner decision D4(b) already fixed it: **`Microsoft.CodeAnalysis.BannedApiAnalyzers` `3.3.4`**,
ticket-named in Scope item 5.

**Disposition.** Accepted. Recorded `3.3.4` as the ticket-approved version in the spec, plan, task list, and
the `Directory.Packages.props` inventory (with no upgrades).
**Edits.** `spec.md` FR-007 / Key Entities / Assumptions; `plan.md` Technical Context, structure, file-impact
table; `tasks.md` T004 and T012.

### H3 — T025 authorizes an out-of-scope CONTEXT.md/ADR edit (Ledger) — **ACCEPT**

**Evidence.** `tasks.md` T025 amended `CONTEXT.md` and `docs/adr/`, neither ticket-named. `CONTEXT.md` is
DEV-292; B4 frozen scope and `PRODUCT.md` §5.6 forbid editing files the ticket does not name; executing T025
would break T024/SC-005.

**Disposition.** Accepted — **removed** (deferred to a separately authorized follow-up). The
`harness.yml`-vs-MSBuild `TreatWarningsAsErrors` reconciliation it also carried is already recorded in
`spec.md` and `plan.md` (spec PR documentation, not a repo edit).
**Edits.** `tasks.md` T025 deleted, `[P]`/dependency/strategy references renumbered; `analyze.md` coverage
and metrics.

### S1 — T002 must be a hard prerequisite to enforcement, not only remediation (Sentry) — **ACCEPT**

**Evidence.** T002 gated only T013/T014/T018–T020. The enforcement tasks (T011 banned-symbol rewrite + T012
analyzer wiring, T016 strict settings) were ungated, so configuration could enable the gates and merge while
the 35 files were still unauthorized — a red tree.

**Disposition.** Accepted. T002 now hard-gates T011, T012, T015, T016, T021 **and** the remediation tasks,
and blocks push/merge. The Phase-2 header, gate note, checkpoint, dependency section, and strategy all state
this.
**Edits.** `tasks.md` gate note, Phase 2 header/checkpoint, T002 text, dependency section, Implementation
Strategy; `spec.md` US3 "Why this priority" implied, Assumptions; `plan.md` Constraints; `analyze.md` §3/§6.

### S2 — T021 needs a non-incremental / fresh-checkout build proof (Sentry) — **ACCEPT**

**Evidence.** T021 accepted `dotnet build LamuFlix.sln`, which a warm incremental build can satisfy without
recompiling the remediated files — not proof of a green strict build.

**Disposition.** Accepted. T021, SC-001, and the US3 independent test now require
`dotnet build LamuFlix.sln --no-incremental` (or a clean-checkout build).
**Edits.** `tasks.md` T021; `spec.md` SC-001 and US3 Independent Test; `plan.md` Constraints.

---

## MEDIUM

### M1 — Task.WaitAll retention specified with incompatible literals (Ledger) — **ACCEPT**

**Evidence.** `tasks.md` T011 wrote `M:System.Threading.Tasks.Task.WaitAll(...)` (literal ellipsis), which
does not match the committed row. Verified in `BannedSymbols.txt`:
`M:System.Threading.Tasks.Task.Wait;Sync-over-async deadlocks under a synchronization context. Await the task.`
and `M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[]);Sync-over-async. Use await Task.WhenAll(...).`

**Disposition.** Accepted. Both rows are now quoted with full IDs and marked **byte-for-byte verbatim** in
all artifacts.
**Edits.** `tasks.md` T011; `spec.md` FR-006, SC-006, US2 Independent Test; `plan.md` structure/file-impact.

### M2 — T001 cannot produce its claimed banned-symbol baseline (Ledger) — **ACCEPT**

**Evidence.** T001 attributed "3 banned-symbol hits" to the two build commands, but `BannedApiAnalyzers` is
not wired until T012 and no symbol scan was listed — the baseline could be reported falsely.

**Disposition.** Accepted. T001 now specifies **two separate, non-mutating** measurements: (a) the two build
counts, and (b) an independent source scan for the five symbols, reported separately.
**Edits.** `tasks.md` T001; `plan.md` Implementation Strategy step 1.

### M3 — T022 does not constrain `dotnet format` (Ledger) — **ACCEPT**

**Evidence.** `dotnet format --verify-no-changes` with no workspace/include can scan the whole repository or
select the wrong workspace, producing unrelated failures and no proof for the authorized files.

**Disposition.** Accepted. T022 and spec US3 scenario 3 now scope it to the solution and the exact authorized
include set.
**Edits.** `tasks.md` T022; `spec.md` US3 acceptance scenario 3.

---

## COMPASS (Spec axis)

### C1 — 35-file envelope is imprecise (34 `.cs` + 1 Razor view) — **ACCEPT**

**Evidence.** Counting `brief.md` §Appendix: 33 nullable/analyzer files = 16 Data `.cs` + 14 Web source
(13 `.cs` + `LamuFlix.Web/Views/Filmes/Index.cshtml`) + 3 Test `.cs`; plus 2 banned-symbol `.cs` = 35 source
files, of which **34 are `.cs` and 1 is a Razor view**.

**Disposition.** Accepted. Every artifact now states "35 source files (34 `.cs` + 1 Razor view)" and the
per-project split.
**Note.** `CONCLUSIONS.md` B1 still reads "35 unnamed `.cs` files"; it is append-only and preserved, so the
correction lives here and in the generated artifacts, not in the record.
**Edits.** `spec.md` Gate 1 bullet, authorization table, FR-009, SC-005, Assumptions; `plan.md`
Constraints/Scale/Scope, gated block, B1 envelope; `tasks.md` gate note, T019/T020, T024; `analyze.md` §3.

### C2 — retain CA1502 extraction-only — **ACCEPT (already present; retained)**

**Evidence.** CA1502 appears among the authorized analyzer fixes in `spec.md` FR-009, `plan.md` §B1
envelope, and `CONCLUSIONS.md` B1 implication 2 with the "by extraction only" qualifier.

**Disposition.** Accepted. No artifact dropped it; the qualifier is now explicit in `spec.md` FR-009 and
`plan.md` §B1 envelope. It is retained unchanged.
**Edits.** `spec.md` FR-009; `plan.md` B1 envelope.

### C3 — no literal ellipsis in the Task.WaitAll ID — **ACCEPT**

**Evidence.** Same defect as M1 (`Task.WaitAll(...)`).

**Disposition.** Accepted; resolved together with M1.
**Edits.** `tasks.md` T011; `spec.md` FR-006/SC-006/US2; `plan.md`.

---

## LOW

### L1 — I1/I2 have no B3 disposition (Ledger) — **ACCEPT**

**Evidence.** `analyze.md` §5 recorded I1 and I2 as LOW but neither fixed nor accepted, leaving the
review-loop record incomplete (B3 requires Low findings be recorded and either fixed or accepted as a
tracked follow-up).

**Disposition.** Accepted. Both are now marked **accepted as tracked follow-ups** with owners: I1 requires
no artifact change; I2 is owned by the constitution's epic-retirement order.
**Edits.** `analyze.md` §5.1.

---

## Residual blockers (unchanged, and now harder to bypass)

1. **B1 — Scope item 6 (PRIMARY).** The owner must paste the proposed Scope item 6 into DEV-289 in YouTrack.
   Until then, the 35 source files are unauthorized, the enforcement configuration (T011/T012/T016) and its
   proofs (T015/T021) must not be started, pushed, or merged, and Gate 1 stays closed.
2. **Owner action, not a repo edit.** T002 is a YouTrack write outside the implementation worktree.
3. **No other blocker.** All plan-challenge HIGH/MEDIUM findings are resolved in the generated artifacts;
   the two LOW informational findings are accepted and tracked.

## Read-back

- `brief.md` and `CONCLUSIONS.md`: untouched (verified by path; not in the `git status` change set below).
- Generated artifacts amended: `spec.md`, `plan.md`, `tasks.md`, `analyze.md`,
  `checklists/requirements.md`, `plan-challenge-adjudication.md` (new).
- Counts re-verified against the tree: SDK `10.0.400` installed; 14 `Version` attributes (Web 2, Data 1,
  Worker 5, Test 6) plus 1 in `Directory.Build.props`; exact `Task` row IDs read from `BannedSymbols.txt`.
- Boundary intact: no ticket-named file was changed; no `.cs` was touched; no gate threshold lowered.

## No-commit receipt

No `git add`, `git commit`, `git push`, PR, or branch operation was performed. Only files under
`specs/DEV-289/` were written. See the `git status` read-back in the hand-back message.
