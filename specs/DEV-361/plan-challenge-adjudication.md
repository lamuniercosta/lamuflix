# DEV-361 plan challenge: Round 2 adjudication (final)

**Adjudicator:** Keel · **Date:** 2026-09-23 · **Base:** `main` `5a38215` · **Sources:** `findings-DEV-361-Sentry-R2`, `findings-DEV-361-Compass` (Round 2), `findings-DEV-361-Ledger` (Round 2)

Round 1 (T013) is superseded by the owner's B1 scope change. This is Round 2 of 2. There is no Round 3.

| ID | Sev | Ruling | Resolution |
|---|---|---|---|
| Compass F1 | MED | Accepted | Fix-first (AC3) is carried in FR-008, SC-005, US3, the `tasks.md` Phase 4 header, T017, the Implementation strategy, and `plan.md`. Patron's worked form `is { } formats` at `FilmesServices.cs:106` is illegal in the EF expression tree (CS8122), so the form is left to T017: it must compile as an expression tree and select the same row and `Path` for every input, or else the bracket applies. Patron ruled that §2.3 item 5 does not fire. |
| Compass F2 | LOW | Accepted | P3 is ruled by Patron: the three WorkerTests hits move in (FR-009). The measured baseline of 39 is preserved in DEV-361, and 36 remain in DEV-281/DEV-366, with no double count. Rigger records it under T003 (`plan.md` P3). |
| Compass F3 | LOW | Accepted | The stale `spec.md` YouTrack line is dropped. The ticket-side B1/T020 wording is Patron → Rigger, and this ticket's artifacts are unaffected. |
| Ledger R2 | MED | Accepted (blocked freeze) | The `tasks.md` dependency section now says that under (B), T019 leaves DEV-361, and T004–T017 and T010–T012 wait for the prerequisite. T019 runs only under (A). |
| Sentry R2-F1 | MED | Accepted | T001 adds a read-only feasibility check. The `plan.md` feasibility desk-check has every method ≤6. Lambda `&&` attribution is confirmed by reconciliation to the measured 12. Locals `:40-42` are declared at first assignment in each helper, and that is the only permitted change to a local. The zebra-fallback evidence rule is in T019. The hard stop stands. |
| Sentry R2-F2 | MED | Accepted | T001 baselines `dotnet format` and `dotnet test`. T012 judges only new failures, and pre-existing out-of-scope failures go to Patron. |
| Sentry R2-F3 | LOW | Accepted | `plan.md` step 4 is aligned with the `tasks.md` shared-file sequencing. |
| Sentry R2-F4 | LOW | Accepted | T011 adds a mechanical disable/restore parity and scope check per file. |

No finding adds a §2.3 item, a task, or a chain change. T019/T020, size:M, FR-010, and the (A)/(B) text are unchanged, except for Sentry R2-F1's local-declaration clarification in T019.

**Re-frozen.** The owner answered T020 **(A)** on 2026-09-23 and said to proceed without waiting for the re-spec PR to merge. Gate 1 is open, and Phase B is ready on `feature/DEV-361`.
