# DEV-358 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-01
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-358-spec` · branch `feature/358-spec` · HEAD `c2d78146b359bd17ab36ecaa9f33342272d47ebf`
**Method**: Spec Kit drafting from controlling `brief.md` (grill closed Q12/12, READY FOR QUILL / OWNER DECISION PENDING), then Spec Kit Repair Round 1 from Patron Analyze Round 1 repair rulings. Templates: `.specify/templates/{spec,plan,tasks,checklist}-template.md`. Form precedent: `specs/DEV-424/`, `specs/DEV-331/`. `/speckit-analyze` not run by Quill (Keel owns read-only analyze). `.specify/feature.json` already pointed at `specs/DEV-358` and was left unchanged.

**Inputs**: controlling `specs/DEV-358/brief.md` (Q1–Q12 + Analyze Round 1 repair rulings), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, Maestri notes `DEV-358`, `analyze-DEV-358`, `recon-DEV-358`, `recon-DEV-358-2`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only; no constitution edits).

**Artifacts written (Quill-owned)**:

- `specs/DEV-358/spec.md` — carries verbatim unticked Q6 collection FK checkbox
- `specs/DEV-358/plan.md`
- `specs/DEV-358/tasks.md`
- `specs/DEV-358/checklists/requirements.md`
- `CONTEXT.md` — six glossary entries added (C2)
- this receipt

**Not invented by Quill**: repair decisions C1/C2/I1/I2/I3 (Patron rulings in brief/CONCLUSIONS/ASSUMPTIONS). Quill did not edit Patron-owned brief/CONCLUSIONS/ASSUMPTIONS in this repair pass.

**Owner checkbox**: carried verbatim and unticked from brief Owner decision / ASSUMPTIONS.md / CONCLUSIONS Q6:

- [ ] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?

Gate 1 stays closed until the user answers on the eventual spec PR. Planned path uses recommended FK branch conditionally only.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ticket/ASSUMPTIONS and recon facts named by the ask.

**Path verification (Q11 + C1/C2 amendments)**: Listed tracked production/test/script paths from the amended Q11 manifest were checked at HEAD `c2d78146`; all present. No bounded path correction required in `brief.md`. EF migration timestamp remains tooling-assigned. New files in the manifest remain prospective targets, not existence claims. Core paths use functionality-based names `MovieKeyword.cs` / `MovieSignalSnapshot.cs` / `MovieMetadataSignalTests.cs` (C1). `CONTEXT.md` added to manifest (C2).

---

## Spec Kit Repair Round 1 receipts (2026-11-01)

Sources: `analyze-DEV-358` numbered findings; Patron rulings in `CONCLUSIONS.md` Spec Kit Analyze Round 1 Repair Rulings and amended `brief.md` / `ASSUMPTIONS.md`.

| Finding | Repair applied | Where |
| --- | --- | --- |
| **C1** CRITICAL | Core names updated to functionality-based `MovieKeyword`, `MovieSignalSnapshot`, `ExtendedCast` (`MovieCastEntry`); vendor `Tmdb*` retained on Infrastructure adapters/`TmdbOptions` only | `spec.md` FR-004/FR-007/Key Entities/US2; `plan.md` Approach/Data model/Project Structure/Constitution VIII; `tasks.md` Package 2–6 paths and notes |
| **C2** HIGH | Six glossary entries added to `CONTEXT.md`; glossary task `T002` in Package 2; `CONTEXT.md` in plan Project Structure and Package 2 Approach | `CONTEXT.md`; `tasks.md` T002; `plan.md` Approach step 2 + Structure; `spec.md` Local vocabulary |
| **I1** HIGH | Formal folding authorization recorded: `research.md`, `data-model.md`, `quickstart.md`, `contracts/tmdb-signals.md` folded into `plan.md`/`spec.md` per Patron ruling; standalone paths omitted | this receipt; `plan.md` Artifacts / Data model (folded; I1-authorized) |
| **I2** MEDIUM | Task dependencies aligned to strict 8-package sequential ordering; parallelism only within a ready package; forbidden early overlaps removed | `tasks.md` Packages 1–8 + Dependencies table; `plan.md` Approach checkpoints |
| **I3** MEDIUM | Removed red-first / failing-then-passing clause from `SC-005`; retained automated coverage + applicable gate evidence; TDD remains optional | `spec.md` SC-005; `plan.md` Test Strategy; `tasks.md` Tests header |

**I1 formal folding authorization (verbatim basis)**: Patron Analyze Round 1 repair ruling — "(I1) Authorize folding `research.md`, `data-model.md`, `quickstart.md`, and `contracts/tmdb-signals.md` into `plan.md` and `spec.md` per DEV-331/DEV-424 house precedent and remove those four standalone paths from the Q11 manifest (including `DRAFTING_RECEIPT.md` instead)." Cited in `CONCLUSIONS.md` Spec Kit Analyze Round 1 Repair Rulings and `brief.md` Planned exact edit set / Full Patron answer register repair block.

**Encoding notes (not new decisions)**:

- Assumed TMDb keyword/credits shapes and evaluation plumbing remain tagged [assumed] via ASSUMPTIONS.md citations; not claimed as verified external documentation.
- No separate ADR authored (Quill never writes ADRs).
- Task IDs renumbered in Repair Round 1 so Package 2 glossary is `T002` and measurement is `T028` (gates `T029`–`T038`); coverage remapping belongs to Keel's next analyze pass.

### needs decision:

- *(none for Quill drafting/repair — the open Q6 structural owner checkbox is already recorded verbatim in spec.md and is the user’s Gate 1 answer, not a Quill inventable decision.)*

**Clarify**: coverage Clear for Quill-owned drafting/repair; no `[NEEDS CLARIFICATION]` markers; Q6 remains the sole unticked owner checkbox.

**Status**: Spec Kit artifacts drafted and Repair Round 1 applied (C1/C2/I1/I2/I3). Gate 1 not approved. PLAN FROZEN not claimed. No production gates, tests, or evaluation metrics run or passed.

**Next**: Keel read-only repair verification / re-analyze; three-axis plan challenge; Quill applies any further numbered fix list only.

---

## Spec Kit Repair Round 2 receipts (2026-11-01)

Sources: `analyze-DEV-358` Round 2 numbered fix list (lines 110–116); residual finding I2 only. No new Patron decision or Q13.

| Finding | Repair applied | Where |
| --- | --- | --- |
| **I2** MEDIUM (residual) | Checkpoint 7 unlocks Package 8 only when `comparison.md` satisfies Q10 and T028 is complete; BLOCKED, FAIL, or incomplete measurement leaves Checkpoint 7 unsatisfied and Package 8 locked. T028 acceptance bar and all other package dependencies unchanged. | `tasks.md` Checkpoint 7 (Package 7) |

**Q6 preservation**: verbatim unticked owner checkbox retained; Quill did not edit brief/CONCLUSIONS/ASSUMPTIONS or tick the checkbox.

### needs decision:

- *(none for Quill Round 2 repair — residual I2 is propagation of the already-ratified strict package order.)*

**Status**: Spec Kit Repair Round 2 applied (residual I2). Gate 1 not approved. PLAN FROZEN not claimed. Analysis CLEAN not claimed.

---

## Plan Challenge Round 1 repair receipts (2026-11-01)

Sources: Maestri note `adjudication-DEV-358-plan`; controlling `brief.md:309+` disposition register and R1–R10; `CONCLUSIONS.md:255+` (complete Patron P1–P12 + confirmed correction + verbatim Q6); `ASSUMPTIONS.md` Plan Challenge Round 1 controlling amendments. Quill did not edit Patron-owned brief/CONCLUSIONS/ASSUMPTIONS. Historical Q1–Q12 exchanges left untouched.

**Owner checkbox preserved verbatim and unticked**:

- [ ] blocked: structural — Should DEV-358 retain the landed DEV-424 collections table and nullable movies.collection_id FK as the sole collection-membership persistence (adding only keyword persistence/join table and migration) instead of the collection join table named in DEV-358 Scope and Technical Design, or transition collection membership to a join table?

| Repair | Applied | Where (exact deltas) |
| --- | --- | --- |
| **R1** | Per-signal Available/KnownEmpty/Unavailable/Disabled save table; nullable CollectionId alone never clears; DEV-424 clear-on-OMDb-success superseded for unavailable/disabled collection; KnownEmpty clears retained; amended collection proof + equality/dirty-tracking/mixed-state proofs | `spec.md` US2/FR-005/FR-006/Edge Cases; `plan.md` Data model / Risks / Approach step 4; `tasks.md` T015/T016 |
| **R2** | Tracked-load keyword/full-cast joins + related identities before replace; deterministic keyword-ID namespace locks and base/FullCast actor natural keys; reload-save / concurrent same-keyword / mixed-state / atomic rollback | `plan.md` Approach step 4 / Data model / Risks; `tasks.md` T013/T015/T016; `spec.md` US2 scenarios |
| **R3** | `FullCast` amends C1 `ExtendedCast`; `ImmutableHashSet<string>` cast; `ImmutableArray<MovieKeyword>` snapshots; `ImmutableHashSet<int>` feature keywords; nested availability enum in `MovieSignalSnapshot.cs`; no `MovieCastEntry`; snapshot collection availability only; identity on `MovieMetadata.CollectionId`/`CollectionName` | `spec.md` Local vocabulary / FR-004/FR-007 / Key Entities; `plan.md` Constitution VIII / Approach step 2 / Data model; `tasks.md` T003–T008; checklist domain items |
| **R4** | One logical invocation identity→details→keywords/credits; typed supplemental NotFound/Failed keeps Found/MarkEnriched; cancellation/unexpected propagate; no-primary-Found matrix preserved; Constitution IV corrected to state machine / atomic claim / Core decisions / thin consumer / no raw exception storage | `spec.md` US1/FR-001/FR-003; `plan.md` Constitution IV / Approach step 3 / Internal contract; `tasks.md` T009/T010/T012 |
| **R5** | Removed planned `UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs`; WireMock cases in `IntegrationTests/MetadataProviderTmdbSignalTests.cs`; independent parse + large-cast smoke; Shouldly/NSubstitute/AutoFixture/Theory/MemberData named; zero raw api_key URI logging | `plan.md` Project Structure / Test Strategy; `tasks.md` T011/T012; `spec.md` FR-012/FR-014 |
| **R6** | Tracked receipt `specs/DEV-358/comparison.md`; raw JSON ignored under `artifacts/evaluation/DEV-358` (never force-add); full receipt fields; profile replay does not prove seed grouping; T023/SC-003 mandatory; Package 8 BLOCKED/FAIL lock retained; no invented metrics now | `spec.md` US4/FR-010/SC-002; `plan.md` Structure / Approach step 7 / Quickstart; `tasks.md` T028 / Checkpoint 7 |
| **R7** | Added `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs`; child-only strict mode → pure `EvaluationReplayOptions.Default`; unset=Disabled; reject invalid; explicit options environment-independent; no EvaluationRunner | `plan.md` Structure / Approach step 6; `tasks.md` T024/T026/T027; `spec.md` US4/FR-010 |
| **R8** | Enriched fixture explicitly adds keyword=1 and collection=2; identical positive weights across paired modes; Enabled independently exercises keyword and collection terms; empty/no-op/cast-only insufficient | `spec.md` US4/FR-010/SC-002; `plan.md` Data model / Approach step 6; `tasks.md` T024/T025/T027/T028 |
| **R9** | Incremental enrichment / no automatic backfill documented; embedding-less eligible local union; existing missing-plot fallback; pure grouper no fourth port; leading-block taste cost retained; production/API seed proof separate from profile replay | `spec.md` Scope/US3/FR-009/FR-015/SC-001/SC-003; `plan.md` Approach step 5 / Risks; `tasks.md` T017/T019–T021/T023 |
| **R10** | Retained `movie_tmdb_cast` and `TmdbSignalReplayTests` provenance rationale; README/CHANGELOG excluded / narrative residual noted no ticket; no constitution departure or new owner escalation; checklist + this receipt updated; Q6 verbatim unticked; no PLAN FROZEN / Gate 1 / build pickup / gate-metric PASS claimed | this receipt; `checklists/requirements.md`; `spec.md` Out of scope/SC-006; `tasks.md` Notes/T029 |

**Manifest deltas vs prior Spec Kit draft (exact)**:

- Add: `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs`
- Add (Phase B tracked receipt target): `specs/DEV-358/comparison.md`
- Remove: `tests/LamuFlix.UnitTests/Adapters/TmdbMetadataProviderSignalTests.cs`
- Retain ignored only: `artifacts/evaluation/DEV-358/{baseline.json,tmdb.json}`
- Rename live identifier: `ExtendedCast` → `FullCast` (historical Q1–Q12 answer texts untouched)

**23 finding → repair mapping** (from adjudication; all dispositioned; none rejected): Sentry H1→R1; H2→R2; M1→R1/R2/R3; M2→R4; M3→R2/R5; M4→R5; L1→R5/R6; L2→R9; L3→R6/R9; Ledger F1→R6; F2→R4; F3→R5; F4→R3; F5→R3; F6→R3/R5; F7→R10; Compass F1→R7; F2→R8; F3→R4; F4→R9; F5→R3; F6→R9; F7→R10.

### needs decision:

- *(none for Quill Round 1 plan-challenge repairs — open Q6 structural owner checkbox remains the user’s Gate 1 answer, verbatim unticked.)*

**Status**: Plan Challenge Round 1 repairs R1–R10 applied to Quill-owned derived artifacts. Five High findings remain pending Keel read-back / closure. Gate 1 not approved. PLAN FROZEN not claimed. No production gates, tests, or evaluation metrics run or passed. Hard two-round plan-challenge cap retained.

**Next**: Keel read-back of every R item; Quill applies any further numbered fix list only if Round 2 is opened under the hard cap.

---

## Plan Challenge Round 2 repair receipts (2026-11-01)

Sources: Keel closure read-back residual R7 factual wording (Compass F1); one numbered Round 2 fix list item only. No new Patron decision, Q13, or scope. Historical Round 1 text above preserved (including R7 "Added" wording and Manifest "Add: EvaluationEntry.cs" as Round 1 history). Hard two-round plan-challenge cap retained.

| Finding | Repair applied | Where |
| --- | --- | --- |
| **R7** residual wording (Compass F1) | Describe extending existing `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` (already at HEAD); reserve prospective status for `comparison.md` and genuinely new targets only | `plan.md` Approach step 6; Structure tree comment; Structure Decision; `tasks.md` T024 |

**Exact changed lines (Round 2)**:

- `plan.md` Approach step 6: `Add \`tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs\`` → `Extend existing \`tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs\``
- `plan.md` Structure tree comment: `# R7 bounded correction` → `# existing; R7 extend (not prospective)`
- `plan.md` Structure Decision: removed `EvaluationEntry.cs` from prospective pair; stated it exists at HEAD as R7 extend target; prospective reserved for `comparison.md` and genuinely new targets
- `tasks.md` T024: `Add \`tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs\` and extend` → `Extend existing \`tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs\` and`

**Q6 preservation**: verbatim unticked owner checkbox retained; Quill did not edit brief/CONCLUSIONS/ASSUMPTIONS or tick the checkbox. All prior decisions, Spec Kit Repair Rounds 1–2, and Plan Challenge Round 1 R1–R10 receipts left intact.

### needs decision:

- *(none for Quill Plan Challenge Round 2 residual R7 wording — open Q6 structural owner checkbox remains the user’s Gate 1 answer, verbatim unticked.)*

**Status**: Plan Challenge Round 2 residual R7 factual wording applied. Gate 1 not approved. PLAN FROZEN not claimed. Hard two-round plan-challenge cap retained (Round 2 used).

**Next**: Keel re-read and freeze if clean.
