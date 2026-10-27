# DEV-331 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-26
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-331-spec` · branch `feature/331-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask with `$env:PYTHONUTF8=1`. `setup-plan.ps1 -Json` resolved `specs/DEV-331` (created plan stub). `setup-tasks.ps1 -Json` deferred until `spec.md` existed; artifacts filled from templates and DEV-303 precedent. `feature.json` already pointed at `specs/DEV-331` and was left unchanged.

**Inputs**: frozen `specs/DEV-331/brief.md` (Q1–Q8, Q3a, Q9; open Q7 owner checkbox), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, Maestri notes `recon-DEV-331` and `recon-DEV-331-2`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates only; no edits).

**Artifacts written**:

- `specs/DEV-331/spec.md`
- `specs/DEV-331/plan.md`
- `specs/DEV-331/tasks.md`
- `specs/DEV-331/checklists/requirements.md`
- this receipt

**Owner checkbox**: consolidated Q7 text carried verbatim at the top of `spec.md` (and mirrored in plan Constitution Check / Complexity Tracking). Gate 1 remains closed. No ADR authored (Quill never writes ADRs); amendment/ADR coordination remains with Conductor.

**Decisions invented by Quill**: none. Technical behaviour taken only from brief/CONCLUSIONS. Drafting-only layout choices the brief delegated (test/helper filenames, two health files, fixture directory) are recorded in `plan.md` § Project Structure; PC1–PC6 supersede earlier UnitTests mapping/fixture placement.

**needs decision:** none.

**Verification**: not run (Phase A drafting only; no build/test gates).

---

## Plan-challenge repair round 1 of max 2 (2026-10-26)

**Authority**: PC1–PC6 Patron ruling in `CONCLUSIONS.md` / brief append-only addendum (commit `c6ede338`). Owned files only: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. Brief/CONCLUSIONS/ADJUDICATION/production untouched. Task IDs T001–T034 and Q7 owner checkbox kept verbatim.

**Repairs applied (Keel numbered fix list 1–12)**:

1. **Sentry F1 / PC1** — FR-011/T015 normalize TMDb `BaseAddress` trailing slash at registration only; T018 slashless-loopback outgoing `/3/` path assertion.
2. **Sentry F2 / PC2** — FR-005/US1.7/T010/T013 exact-one-leading-slash poster path (no scheme/host/backslash/control/dot segments); non-throwing `TryCreate` else absent; adversarial fixtures in T001/T013.
3. **Sentry F3** — SC-003/T020 default HttpClient factory logging stays enabled; sweep covers adapter + `Microsoft.Extensions.Http` + resilience categories on retries/errors, structured state, exceptions, LamuFlix/`System.Net.Http` activities, health descriptions.
4. **Sentry F4/F6 / PC3** — FR-006/US2.5/US2.8/T014/T019/T021: caller `OperationCanceledException` when `ct.IsCancellationRequested` propagates unrecorded (coincident cancel included); non-caller timeout/breaker → ProviderUnavailable; search 404 → InvalidResponse; explicit tests. F5 atomic immutable last-write-wins snapshot in FR-010/T007; F7/F8 accepted shared-tuning/reactive residuals noted in plan/spec edges.
5. **Ledger M1 / Compass F5 / PC4** — Mapping tests moved to IntegrationTests via `FindAsync` + real typed client/WireMock; fixtures colocated under `IntegrationTests/Fixtures/SecondaryMetadata/` with same-project CopyToOutputDirectory strategy; no stub handler / InternalsVisibleTo; all unit-mapping claims updated; T013 depends on T001 + adapter + registration + probe.
6. **Ledger M2 / PC5** — T022 owns dedicated `SecondaryMetadataProviderTelemetryTests.cs`; T017 bounded dedicated secondary probe; no OMDb probe rewrite / shared abstraction.
7. **Ledger M3/M4 / PC5** — Neutral `SecondaryMetadataProvider*` health/test/helper/options names retained; cited PC5 / Principle VIII / `recon-DEV-331:45`; isolation + blank-key/`IOptions` divergence justify small duplication.
8. **Compass F1/F2 / PC6** — T002/FR-012 private nested `AllowedBaseUrlAttribute` in `TmdbOptions` only; T008/FR-010 explicit `IOptions<TmdbOptions>` on health check; T009 initial + disabled blank-key Unhealthy tests.
9. **Ledger L1 / Compass F3** — `[P]`/deps repaired (T005/T009 not `[P]`; T003 no T001 dep; T011 requires T002/T003/T007; T013 after fixture+adapter+registration+probe); US5/US6 ownership corrected (T016→US6); T024 exact `HealthCheckRegistrationTests.cs` + `IntegrationTests/HealthEndpointTests.cs`; T025 exact `IntegrationTests/MetadataProviderCompositionTests.cs` + `UnitTests/Options/OptionsStartupValidationTests.cs`; unnamed related/as-required path scope removed; T026 precedes implementation (Phase 0).
10. **Compass F4** — T023 owns inspection that `AddMetadataProvider` does not invoke `AddLamuFlixOptions`.
11. **Ledger L2/L3** — TDD optional; fixture-first and all required tests before verification; preserve unrelated existing comments; no new explanatory comments; BannedSymbols via existing build/analyzers (no separate task).
12. **Compass F6** — FR-009/US5.5/T011/T022: no new ActivitySource/Meter **name**; own `ActivitySource` using `TelemetryConstants.ActivitySourceName` allowed; existing histogram reused.

**Coverage**: all twelve fix-list items applied to owned drafts. No new decisions. No broad recon. Others’ edits (including Patron commit `c6ede338`) preserved.

**Next**: Keel re-read / plan-challenge re-check against repaired drafts.

---

## Plan-challenge repair round 2 FINAL of max 2 (2026-10-26)

**Authority**: PC7 Patron ruling in `CONCLUSIONS.md` / brief append-only addendum. Owned files only: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. Brief/CONCLUSIONS/ADJUDICATION/production untouched (Keel-owned). Task IDs T001–T034 and Q7 owner checkbox kept verbatim. No csproj/production edits in this drafting pass.

**Repairs applied (Keel numbered fix list — 1 item)**:

1. **PC7 fixture-copy exact scope** — Named `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` in FR-015, plan Project Structure tree / loading strategy, and T001 as a **future Phase B edit ONLY** to add `Fixtures/SecondaryMetadata/**/*.json` with `CopyToOutputDirectory=PreserveNewest` (preserving relative output directories); no package/project-reference/other-fixture/settings changes. Fixture reads locked to `Path.Combine(AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", …)` in FR-014, plan loading strategy, and T013. Removed every “or equivalent” fixture-loading option from FR-014, plan, and T001.

**Exact repaired loci**:
- `spec.md` FR-014 (load path + csproj reference; no “or equivalent”); FR-015 (named csproj future edit); Status/Input/Precedence/Scope/Traceability PC7 cites
- `plan.md` Repair banner; Project Structure `LamuFlix.IntegrationTests.csproj` line; loading-strategy paragraph (PC7)
- `tasks.md` Repair banner; T001 (named csproj + PreserveNewest + AppContext reads); T013 (AppContext.BaseDirectory load path)
- `checklists/requirements.md` round-2 FINAL note + PC7 readiness item
- this receipt round-2 section

**Coverage**: single PC7 fix-list item applied. No other decisions or rewrites. No recon.

**Next**: Keel final plan-challenge read-back under round cap.
