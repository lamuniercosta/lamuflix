# DEV-438 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-04
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-438-spec` · branch `feature/438-spec`
**Method**: Spec Kit specify → plan → tasks → requirements checklist from the closed grill; then Phase 2 repair round applying Keel's single consolidated numbered list (`DEV-438-keel-quill-phase2-fixes.txt`) after Patron Phase 2 rulings in `CONCLUSIONS.md` and brief Phase 2 adjudication addendum. Grill already closed (`brief.md` Q8); mandatory before_specify grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge / freeze).

**Inputs**: frozen `specs/DEV-438/brief.md` (Q1–Q8 + Phase 2 adjudication addendum), `CONCLUSIONS.md` (Q1–Q8 + Phase 2 rulings 1–8), `ASSUMPTIONS.md`, Maestri notes `DEV-438`, `recon-DEV-438` (203 lines), `recon-DEV-438-2` (77 lines), live `scripts/get-task.ps1 DEV-438`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` v1.10.0 (cite only). Existing composition/test filenames resolved from refreshed recon (scanner, ports, Movie, playback handler/adapters, HandlerRegistration, ApiEndpoints/PlaybackEndpoints/ImportEndpoints, DirectoryMediaLibraryScannerTests, ImportMovieFolderCommandHandlerTests, ImportEndpointTests, PlayMovieCommandHandlerTests, launcher tests, PlaybackEndpointTests, OpenApiContractTests, ApiEndToEndImportTests / ApiEndToEndPlaybackTests).

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-438/spec.md`
- `specs/DEV-438/plan.md`
- `specs/DEV-438/tasks.md`
- `specs/DEV-438/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADRs, constitution, production code, tests, `harness.yml`, generated OpenAPI/TS/MSW, `CONTEXT.md` (delivery task only).

**Owner checkbox**: none — brief / Phase 2 addendum record no ticket contradiction and no constitution departure. Owner Gate 1 remains pending.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS/ticket/recon facts and Patron Phase 2 rulings. Freeze left to Keel.

**Phase 2 repair items applied (Keel list 1–14)**:

1. Sentry F1 — unexpected candidate IO → generic 500 / bulk abort; no silent skip; T002/T004 fault case; risks + spec.
2. Sentry F2 — resolve-to-launch launcher failure → 500, zero history/Format; T016/T018; no disappeared-media detection promise.
3. Sentry F3 + Compass F1 — RequestAborted + cooperative pre-resolve/launch/save; FR-004/FR-010; US2/US4; T007/T009/T015/T016; bulk cancellation never failed.
4. Sentry F4 + Ledger M3 — Baseline/From/Apply Format; T010 concurrent enrichment + no UPDATE unchanged; scalar ID snapshot; T013–T015.
5. Ledger M1/M2 — playback dependency list + Patron assessment; shared `RescanMovieOperation`; decorators at entry.
6. Ledger M4 — import 500→404/409 (incl. only-excluded); Scan/ResolvePrimaryVideo share; ImportEndpoints + T004/T006; FR-008/US1.
7. Sentry F5 + Ledger M5 — cite both recon notes; name `PrimaryVideo.cs`, results/responses, `RescanMovieOperation`; T001 STOP retained.
8. Sentry F6/F7 — legacy sample-only accepted; sync bulk duration accepted (cancellation only).
9. Ledger L1/L2 + Compass F5 — Constitution Check III/VI N/A; MapRescanEndpoints grouping; T024 standalone build then full tests; expected-only catch; T012.
10. Ledger L3/L4 — non-mutable static init; Theory/MemberData + AutoFixture/Faker.Net; property coverage; unique temp dir cleanup.
11. Ledger L5/L6 — glossary Rescan vs Import; T005 test-first where feasible; parallel only disjoint after prereqs.
12. Compass F2/F4/F6 — FR-007 any unexpected abort; MovieId wire form; FR-006 post-snapshot inserts wait; FR-003 no title/year/enrichment; route-collision premise rejected.
13. Contract workflow — `LAMUFLIX_REGENERATE_OPENAPI` + `npm run generate:api` + `check:api`; forced snapshots/mocks footprint.
14. Gate 1 pending; plan awaits Keel freeze; receipt/checklist = repair round; code tasks unchecked.

**Encoding notes (not new decisions)**:

- Concrete names from Phase 2 ruling 7 / ASSUMPTIONS.md: `PrimaryVideo`, `RescanMovieResult`, `RescanLibraryResult`, `RescanMovieResponse`, `RescanLibraryResponse`, `internal sealed class RescanMovieOperation`.
- IMovieRepository ascending scalar ID-snapshot described by role; exact method name left to Build inside existing port/adapter files (PresentIds pattern per recon).
- Exact new unit/integration test filenames under existing test project folders deferred to Build (brief Q6).
- No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; routes/fields live in spec + generation tasks.
- Pre-existing `harness.yml` modification preserved and excluded from staging (brief Status; recon).
- Proposed new paths distinguished from verified existing recon paths in plan Project Structure.

**needs decision:** none.

**needs recon:** none invented by Quill. Build-time pickup recon (T001) refreshes landed-main facts without inventing rulings; STOP on missing facts.

**Clarify**: coverage Clear; no `[NEEDS CLARIFICATION]` markers; no owner Gate 1 checkbox.

**Status**: Phase 2 repair round complete on Quill-owned artifacts. Gate 1 not approved. Quill does **not** claim PLAN FROZEN, CLEAN, or implementation clearance — freeze left to Keel. No production gates, tests, commits, or PR run. Next: Keel verifies cross-artifact consistency and axis dispositions before recording PLAN FROZEN.
