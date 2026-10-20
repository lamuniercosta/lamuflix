# DEV-341 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-20
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-341-spec` | **Branch**: `feature/341-spec` | **HEAD at drafting**: `87b28a934a56d1a1822a2d7133e07f3d53a03e6b`

**Method**: `/speckit-specify`, `/speckit-plan`, and `/speckit-tasks` followed from the skills and `.specify/templates/`. `PYTHONUTF8=1` was set. `setup-plan.ps1 -Json` ran and reported FEATURE_SPEC / IMPL_PLAN under `specs/DEV-341`. `setup-tasks.ps1 -Json` ran after `spec.md`/`plan.md` existed. No `.specify/extensions.yml` hooks were present. Content comes from `brief.md` Q1–Q9, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, notes `recon-DEV-341` and `recon-DEV-341-2`, task note `DEV-341` (including `propertyTests: opt-out`), and `specs/PRODUCT.md`. No decision was made by Quill.

**Artifacts written**:
- `specs/DEV-341/spec.md`
- `specs/DEV-341/plan.md`
- `specs/DEV-341/tasks.md`
- `specs/DEV-341/checklists/requirements.md`
- `specs/DEV-341/DRAFTING_RECEIPT.md` (this receipt)

**Already present (not rewritten by Quill)**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`. Root `CONTEXT.md` already carries **Recommendation** and **Reason**; Spec Kit artifacts cite those terms verbatim and add no glossary edit. `.specify/feature.json` was already pinned to `specs/DEV-341` and is gitignored. Task note `DEV-341` `propertyTests: opt-out` line was preserved (not altered).

**Not produced**: `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `design.md`, ADR (`plan.md` / brief state why). `AGENTS.md` was not edited. No freeze, publish, gates, tests, commit, push, or PR.

**Glossary included in published artifacts**: Recommendation; Reason — definitions match root `CONTEXT.md`. No new term. RecommendedMovieDto called out as API representation only.

**Owner checkbox**: none. Gate 1 is not claimed. Plan freeze is not claimed.

## Post-decision draft synchronization (C1-T1 / C2)

**Date**: 2026-10-20
**Sources**: task note `DEV-341`; `brief.md` Q4 / C1 / C1-T1; `CONCLUSIONS.md` C1 / C1-T1; `analysis-DEV-341`; `recon-DEV-341-3`, `recon-DEV-341-4`, `recon-DEV-341-5`; `.specify/memory/constitution.md` principles I–IX and Compliance `:551`.

**Synced**:
- `spec.md` FR-006 → `ImmutableList<string> Reasons`; `ToImmutableList()` mapping; `ImmutableList<string>.Empty`; never null/default; outer `Results<Ok<List<RecommendedMovieDto>>, BadRequest, NotFound>`; twelve fields; rank order; JSON/OpenAPI/generated TS `string[]`.
- `tasks.md` T003 → same C1-T1 contract.
- `plan.md` Constitution Check lines 45–54 (C2 citations where supported); Design item 3; Complexity Tracking; post-design re-check.
- `checklists/requirements.md` Notes → C1-T1 sync and C2 disposition.

**Preserved**: unrelated draft content; `propertyTests: opt-out` in task note / spec FR-014 / plan Testing / tasks; `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md` untouched by this sync.

## C2-T1 application (scope placement)

**Date**: 2026-10-20
**Sources**: Keel C2-T1 at `brief.md:322-338` / `CONCLUSIONS.md:191-207` (matching exchange); Conductor resume ask.

**Resolved by C2-T1** (placement only; constraints remain in force; numbered principle: **none applicable** for each; no invented I/II/IX citations):
- **C2-a** — Ticket scope / product authority: removed from Constitution Check; under `plan.md` **Scope and prerequisite checks**. Retains `specs/PRODUCT.md` Authority Rules, `recon-DEV-341-2:26-41`, brief Q1/Q8 / C1-T1, zero owner checkboxes.
- **C2-b** — No schema or migration: removed from Constitution Check; under **Scope and prerequisite checks**. Retains care list item 3 and brief Q1 prohibition.
- **C2-c** — Epic sequencing: removed from Constitution Check; under **Scope and prerequisite checks**. Retains `specs/PRODUCT.md` §2, brief Q1, and full Phase B stop condition (user-merged spec PR / Gate 1, Epic 7, landed DEV-340/336–339, Wisp recon; stop on divergence). Prerequisite statement only — not landed-dependency evidence.

**Also applied**: Constitution Check intro replaced with numbered-principle rule plus separate-section reference (`constitution.md:551`); post-design re-check confined to supported numbered-principle gates and cross-references the sibling section; `checklists/requirements.md` Notes synchronized to C2-T1 disposition.

**Preserved**: Keel C1-T1 `ImmutableList<string> Reasons`, ordered `ToImmutableList()` mapping, explicit `ImmutableList<string>.Empty`, no null/default, outer endpoint / twelve-field / rank / JSON-OpenAPI-TS `string[]` contract; `propertyTests: opt-out`; unrelated draft content; `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` not edited by Quill in this ask.

**needs decision**: none remaining from C2-a/b/c.

**needs recon**: none for this sync ask.

**Not claimed**: all of C2 cleared, or artifacts consistent/cleared before later read-only analysis.

**Not done by this ask**: tests, gates, analyzers, format, freeze, plan challenge, commit, publishing.

## Round-1 plan-challenge repair (authorized)

**Date**: 2026-10-20
**Identity**: Worktree `F:\Dev\LamuFlix.worktrees\feature-341-spec`; branch `feature/341-spec`; pinned HEAD `87b28a934a56d1a1822a2d7133e07f3d53a03e6b`. Drafts remain untracked working bytes.
**Sources**: Conductor numbered repair list; notes `DEV-341`, `decision-DEV-341-round1-docs`, `adjudication-DEV-341-plan`, `analysis-DEV-341-2`, `findings-DEV-341-Ledger`, `findings-DEV-341-Compass`, `findings-DEV-341-Sentry`; worktree `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` / `constitution.md` / `PRODUCT.md` read only (not edited).

**Preserved**: Prior C1 / C1-T1 / C2-T1 synchronization; `ImmutableList<string> Reasons` contract; `propertyTests` exit-2-only opt-out; five-phase/Q7 order; C2 scope/prerequisite checks outside Constitution Check; Compass O2 analysis disposition unaltered (factually rejected mapping); other accepted observation dispositions and adjudication scope states.

**Applied (items 1–10)**:
1. F1 Constitution V check records Patron no-departure ruling: HTTP parse/binding/adapter take-range `BadHttpRequestException` → 400 distinct from Core FluentValidation decorator `ValidationException` → 422 (`brief.md:129-135,349-363`; `constitution.md:217-234`; Complexity Tracking + Constitution Check).
2. F2 intermediate expected inventory/drift failures bounded with closing tasks T008 / T009 / T012 / T013; T001 confirms actual affected tests; intermediate commits gate named targeted proofs only; expected failing/deferred never PASS/SKIPPED; Phase 3 closes route/name; Phase 4 and T018 leave no deferred failures; stop rule for unexpected/regression/overdue/unconfirmed.
3. F3 exact fixed prose synchronized in spec FR-008/US3, plan Design 4, T010/T012: `The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.` No numeric maximum/runtime interpolation; transformer receives no runtime options for this description; endpoint validation still uses configured bound.
4. F4 removed literal `-Threshold 6` from T018; normal/refactor complexity use configured `harness.yml` thresholds via supported invocation; exact invocation is Gauge Phase B — unsupported/unestablished → **Could not run**, never substitute a threshold.
5. Compass L2: Q9 `web/src/mocks/recommendations.test.ts` kept as technical/file-envelope placement (spec Assumptions + receipt); no ASSUMPTIONS taste line; no scope/proof change. `ASSUMPTIONS.md` not edited.
6. L1 plan Project Type wording → `ports and adapters, feature-organised core` (PRODUCT/ticket not edited).
7. Ledger O2: `recon-DEV-341:70` and `.specify/memory/constitution.md:485-494` attributed separately in plan Constitution Check.
8. Ledger O3: one cancellation check at shared post-handler/pre-mapping point (spec FR-005 / plan Design 2); mutation no-eligible-project or unestablished gate invocation stops for cited decision / Could not run (T018); no threshold waiver, extra project, duplicate check, or pre-judged mutant equivalence.
9. Ledger O4: Constitution Check adds supported IV (existing status read / no new transitions), VI (existing decorated dispatch / no duplicate host wiring), and I ArchitectureTests inclusion under final `dotnet test`; C2 scope/prerequisite checks remain outside Constitution Check; no invented features or fresh tracing/registration verification claims.
10. Ledger O5: T001 records actual EnrichmentStatus SmartEnum and existing name-converter reuse; stop on divergence; add no converter.

**Edited files only**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`.

**needs decision**: none.

**needs recon**: none for this repair ask (exact supported complexity-gate invocation remains deferred Phase B Gauge obligation per F4).

**Not done**: tests, gates, freeze, plan challenge, commit, publish; no edits to `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, or other files.

## Erratum — C2-T1 receipt source span (Compass L1 concordance)

**Date**: 2026-10-20
**Citation-only correction**: The historical Sources line under **C2-T1 application (scope placement)** above cites `brief.md:322-338`. That span is stale after later appends. Current full C2-T1 decision headed `C2-T1 - Keel plan ruling: scope and prerequisite checks` is `brief.md:330-346` / `CONCLUSIONS.md:191-207`. Concordance recorded at `brief.md:429-433` / `CONCLUSIONS.md:290-294` (decision-DEV-341-round2-docs; adjudication-DEV-341-plan-round2:32-33). Historical receipt source line left intact; citation-only correction; substance unchanged.

## Round-2 M1 documentation sync (authorized)

**Date**: 2026-10-20
**Identity**: Worktree `F:\Dev\LamuFlix.worktrees\feature-341-spec`; branch `feature/341-spec`; pinned HEAD `87b28a934a56d1a1822a2d7133e07f3d53a03e6b`. Drafts remain untracked working bytes.
**Sources**: Conductor bounded round-2 M1 repair ask; notes `adjudication-DEV-341-plan-round2:52-59`, `decision-DEV-341-round2-docs:11-15`, `findings-DEV-341-Ledger-2`, `findings-DEV-341-Compass-2`, `findings-DEV-341-Sentry-2`, `analysis-DEV-341-3`, `quill-sync-DEV-341-round1`; worktree `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` / `constitution.md` / `PRODUCT.md` read only (not edited).

**Preserved**: Prior C1 / C1-T1 / C2-T1 / round-1 repairs; `ImmutableList<string> Reasons`; `propertyTests` exit-2-only; five-phase/Q7 order; configured-threshold rule; FR-005 shared-cancellation wording at `spec.md:96` (no behavioral rewrite); historical receipt sections including the old C2-T1 Sources span; scope/cap/order; zero owner checkboxes.

**Applied (M1 + Compass L1/L2 record status)**:
1. **M1 plan Tests/IX**: Constitution Check Tests/IX gate names approved xUnit v3, Shouldly, NSubstitute, WebApplicationFactory, Testcontainers Postgres, AutoFixture, and Faker.Net; fresh data; Theory + MemberData for repeated take matrices; narrowly justified invariant-builder exception; cites `.specify/memory/constitution.md:318-325,392-393` and round-2 M1 decision. Configured-threshold rule preserved. Technical Context Primary Dependencies / Testing prose aligned to the approved stack without new-dependency or verified-reference claims.
2. **M1 T002**: Appended Theory + MemberData for repeated take/default/bound/precedence matrices; fresh AutoFixture/Faker.Net data; pinned stimuli/expectations; narrow builder exception; existing cases, Shouldly, NSubstitute, AAA, naming, and cancellation/token-forwarding assertions preserved — no case removed, no unlike proofs combined.
3. **M1 T007**: Retained all real-Postgres ranked/missing/Pending cases and collection/seed helpers; fresh AutoFixture/Faker.Net through those helpers; Theory + MemberData only for genuinely repeated scenarios; distinct integration proofs may remain Facts; narrow invariant exception.
4. **M1 T001**: Phase B convention/accessibility confirmation and stop for recon/cited decision if unavailable; no install, invented helper API, or new test-data file.
5. **Design 2**: References round-2 decision appendix `brief.md:410-437` / `CONCLUSIONS.md:271-298` (Compass L2 at `:435` / `:296`); cancellation behavior unchanged.
6. **Compass L1**: Citation concordance already recorded in brief/CONCLUSIONS round-2 appendix; this receipt's C2-T1 erratum above points to that concordance. Nonblocking; no challenge-clearance claim.
7. **Compass L2**: Shared-cancellation clarification already recorded in brief/CONCLUSIONS round-2 appendix; Design 2 cites that appendix; `spec.md:96` left accurate. Nonblocking; no third challenge or clearance claim.

**Edited files only**: `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. `spec.md` not behaviorally rewritten.

**Exact post-edit headings and spans (readback)**:
- `plan.md` Technical Context **Primary Dependencies** `:25`; **Testing** `:29`; Constitution Check Tests/IX gate `:54` (configured-threshold clause retained on same bullet); Design 2 **Pending preflight (Q3)** `:114` (appendix cite; behavior unchanged).
- `tasks.md` T001 `:25`; T002 `:39`; T007 `:63`.
- `checklists/requirements.md` Notes Round-2 M1 bullet `:40`; owner-checkbox bullet `:42`.
- `DRAFTING_RECEIPT.md` historical C2-T1 Sources `:39` (intact); heading **Erratum — C2-T1 receipt source span (Compass L1 concordance)** `:86-89`; heading **Round-2 M1 documentation sync (authorized)** `:91` through end of this section including **Exact post-edit headings and spans (readback)**.
- `spec.md` FR-005 `:96` unchanged (SHA256 matches round-1). Unedited: `brief.md:410-437` / `CONCLUSIONS.md:271-298` round-2 appendix (M1 `:425`/`:286`; L1 concordance `:429-433`/`:290-294`; L2 `:435`/`:296`); `ASSUMPTIONS.md` Q6 taste only.

**needs decision**: none.

**needs recon**: none for this repair ask (Phase B AutoFixture/Faker accessibility remains a T001 confirmation obligation).

**Not claimed**: M1 challenge clearance, all-axis clearance, plan freeze, Gate 1, or third challenge.

**Not done**: tests, gates, analyzers, formatting, freeze, Gate 1, implementation, commit, publish; no edits to `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, source, generated, config, or dependencies.
