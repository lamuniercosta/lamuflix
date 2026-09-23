# DEV-361 — Restore six InspectCode severities

**Ticket:** DEV-361 · size:M (Patron P2: stays M, no L-only mutation gate) · no `ui:` tag · Phase A merged as PR #3. This re-spec is on `feature/361-spec`, and Phase B is on `feature/DEV-361`. **Owner B1 answered [x]**, and the Round 1 freeze is superseded. The re-plan was re-frozen after the Round 2 challenge (T018, 2026-09-23). The owner answered T020 **(A)** on 2026-09-23 and said to proceed without waiting for the re-spec PR to merge. Gate 1 is open.

## Scope and authority

`task-DEV-361:14-30` authorizes five `.editorconfig` rules to return to `warning`, with fixes bounded to the files those inspections flag. The owner answered the B1 checkbox **[x]** on PR #3, and the user confirmed on 2026-09-23 that a checked box is the source of truth. So `resharper_nullable_warning_suppression_is_used_highlighting` becomes the sixth restoration, and the file scope widens past the frozen three files.

Re-measured on unchanged `origin/main` `3cd5802` after DEV-360 merged (see the re-baseline in [research.md](research.md); first measured at `5a38215`):

- **Five-target inspections:** 10 hits in `FilmesViewModel.cs`, `TagHelpers/Extensions.cs`, and `UnitTest1.cs`. Nothing is left outside them: `Details.cshtml:60` no longer fires.
- **B1 inspection:** 112 hits in 18 `.cs` files, groups D/T/M/W, and none in views. That was 114 in 24 at `5a38215`: DEV-360 removed 13 hits and added 11 in `FilmesServices.cs`.
- **AC2 path set:** the union is 25 files. It pulls in 3 pre-existing WARNING+ hits in `WorkerTests.cs`, which DEV-361 now fixes.

## Review loop

Critical or High findings with a concrete failure scenario block. Frozen scope covers:

- the six restorations;
- the 25 measured paths;
- the ticket acceptance criteria, with AC5 answered.

Anything else is a follow-up issue, not a finding in this round. The cap is two review rounds with at most two fix commits per round (`task-pipeline:23-29`; [CONCLUSIONS.md](CONCLUSIONS.md)).

## Gate 1 owner checkbox

- [x] **blocked: structural — restore `nullable_warning_suppression_is_used` to warning (reverses B1)?** Answered [x] on merged PR #3 and confirmed by the user on 2026-09-23. Reverse B1, restore the key to `warning`, and widen the scope.

- [x] **Owner:** blocked: structural — plan change (T019). **Answered (A) on 2026-09-23.** The user's direct answer was relayed by the Conductor, and Phase B does not wait for the re-spec PR to merge. The six-key restoration edits `LamuFlix.Web/Extensions/EntityExtensions.cs`. That puts the pre-existing `DynamicQuery` (CA1502 12) into the threshold-6 refactor gate, and `constitution.md:344-345` requires a helper-extraction fix. Choose one:
  - **(A)** Include T019 in DEV-361: a same-file private-helper extraction of `DynamicQuery`, done as a pure move. **Patron recommends (A).**
  - **(B)** Defer the extraction to a prerequisite follow-up ticket, and block the six-key restoration (T004 onward) until that ticket merges.

## Patron rulings (all ruled; Round 2 re-frozen)

These are Patron items:

- **P1 (ruled):** T019 extraction is constitution-mandated. The Round 1 no-refactor clause is superseded for the 25 paths, and an unremedied failure is a hard stop;
- **P2 (ruled):** size M;
- **P3 (ruled):** the three WorkerTests hits move into DEV-361. The measured 39 is preserved, and 36 remain in DEV-281/DEV-366.
- **F1 (ruled):** fix-first per AC3. §2.3 item 5 does not fire for `FilmesServices.cs:106`. DEV-360 has since resolved `:106` upstream.
- **Item 5, `AssistirFilme` (ruled, re-baseline):** §2.3 item 5 does not fire for a comment-only member bracket at `FilmesServices.cs:74,75`. No executable line changes, and there is no checkbox.

See [plan.md](plan.md).

## Taste assumptions

The suppression wording, local syntax, and `.editorconfig` comment wording are logged in [ASSUMPTIONS.md](ASSUMPTIONS.md).
