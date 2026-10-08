# Tasks: DEV-312 OpenAPI Contract and Scalar Delivery

**Input**: `specs/DEV-312/spec.md`, `plan.md`, `research.md`, frozen `brief.md`

**Prerequisites**: plan.md, spec.md, research.md. No `data-model.md` (no data change), no `contracts/` (the deliverable `web/src/api/openapi.json` is generated at implementation).

**Tests**: Included — the drift test IS the ticket deliverable (US2). Test tasks are implementation, not optional.

**Organization**: Ordered by the frozen sequence (brief Q4): packages and composition → integration test and export path → document and derived snapshot → ADR-0007 and quickstart → controlled drift negative proof → gates. ADR-0007 is Keel-owned; Quill never fills its content.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths in every description

---

## Phase 1: Packages and Composition (Foundational)

**Purpose**: Dependencies and Development-only document/Scalar wiring; blocks everything else.

- [X] T001 [P] [US1] Pin `Microsoft.AspNetCore.OpenApi` 10.0.0, `Scalar.AspNetCore` 2.0.15, and `Verify.XunitV3` 30.3.0 in `Directory.Packages.props` (evidence: research.md D3; implementation restores and reports the real verdict, choosing no versions)
- [X] T002 [US1] Add the required package references in `src/LamuFlix.Api/LamuFlix.Api.csproj`
- [X] T003 [US1] Wire `AddOpenApi("v1")` with an `/api`-path document transformer on `OpenApiOptions` (remove non-business path items; exact path-boundary matching; no MVC reference), plus Development-only `MapOpenApi` and `MapScalarApiReference`, in `src/LamuFlix.Api/Program.cs` (depends on T002; first environment branch; health mappings untouched; `ApiHostCompositionTests.cs` unchanged)
- [X] T004 [P] [US1] Add Verify integration wiring in `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj`

**Checkpoint**: Packages restore; API starts in Development with document and Scalar; Production 404s pending test proof.

---

## Phase 2: Integration Test and Export Path (US1, US3) 🎯 MVP

**Goal**: Real-host proof of Development wiring and Production absence, plus the shared generation path the drift test and regeneration both use.

**Independent Test**: Development host serves `/openapi/v1.json` (3.1, nine path/method pairs) and `/scalar`; Production host returns 404 for both; `ApiHostCompositionTests.cs` unchanged and green.

- [ ] T005 [US1] Create `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` with explicit Development host configuration proving document/Scalar wiring: OpenAPI 3.1, actual generated path/method pairs compared against the expected nine-pair generated set named in plan.md (including `GET /api/movies/{id}`; never derived from generated output or the baseline), retained DTO/status metadata
- [ ] T006 [US3] Add the separate Production host 404 checks for `/openapi/v1.json` and `/scalar` in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`, keeping `Features:LocalPlay` false and never invoking the play route (depends on T005)
- [ ] T007 [US1] Add the shared same-host document export path used by drift comparison and regeneration in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`: HTTP GET of `/openapi/v1.json` from the explicit Development host with the default factory client base URI `http://localhost/` (no custom `BaseAddress`, no port override) used identically by drift, initial export, and regeneration; resolve `web/src/api/openapi.json` and the derived snapshot against the repository-root anchor named in plan.md (stable `Directory.Packages.props` marker; fail closed on missing/ambiguous marker), independently of the process working directory (depends on T005)
- [ ] T008 [US3] Minimal additive `ApiHostFactory.cs` support ONLY if T006 cannot use per-test `WithWebHostBuilder`/`UseEnvironment`; change no config value, LocalPlay flag, health stub, or validation flag (depends on T006)

**Checkpoint**: US1 and US3 proven on real hosts; export path ready for the baseline.

---

## Phase 3: Document and Derived Snapshot (US2)

**Goal**: Committed authoritative baseline plus equality-enforced derived Verify snapshot.

**Independent Test**: Drift test passes on the committed files; any generated-document difference fails with no tracked-file writes.

- [ ] T009 [US2] Generate and commit `web/src/api/openapi.json` through the T007 Development host/document path and repository-root anchor; this explicit initial export is the only step that may create `web/src/api/` and the baseline files inside the verified root (depends on T007)
- [ ] T010 [US2] Add the derived snapshot `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` plus the same-run three-way normalized full-text equality (generated document, committed `web/src/api/openapi.json` content, derived verified file content) asserted before Verify runs in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`; both files resolved against the repository-root anchor named in plan.md independently of the process working directory, and a missing baseline or snapshot file fails the drift test instead of passing; normalization is CRLF→LF on each compared text only; then `Verify(target: <normalized document string>, extension: "json")` with only `UseDirectory("Snapshots")` and `UseFileName("OpenApiContractTests.DriftMatchesCommittedBaseline")`, no other settings and no scrubbers (invocation per research.md D3; depends on T009; active per research.md D3)
- [ ] T011 [US2] Prove deterministic repeated generation in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` (depends on T010)
- [ ] T012 [P] [US2] Add the `.gitignore` `*.received.*` exclusion ONLY if existing rules do not cover received files
- [ ] T013 [P] [US2] Add the `.gitattributes` eol line for `openapi.json` and the derived snapshot ONLY if needed for the fixed normalization

**Checkpoint**: US2 complete — full-document equality, Verify snapshot, determinism; `ApiHostCompositionTests.cs` still unchanged and green.

---

## Phase 4: ADR-0007 and Quickstart (US4; ADR Keel-owned)

**Goal**: Accepted ADR plus the documented explicit regeneration command.

- [ ] T014 [US4] Keel authors `docs/adr/ADR-0007.md` (Status Accepted; house shape per recon:66 — Context, Decision, Consequences, ticket/date/status; records dependency/runtime-generation choice, Development exposure, drift authority, regeneration reference). NOT Quill; NOT implementation.
- [ ] T015 [US4] Add the opt-in-gated regeneration entry in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`: skipped unless `LAMUFLIX_REGENERATE_OPENAPI=true`, filter identity `FullyQualifiedName~OpenApiRegeneration` matching quickstart.md, same host/base-URI/path/anchor as T007, writes those same normalized bytes to `web/src/api/openapi.json` plus `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together (creation of missing output files is allowed only on this explicit opt-in path); CI never sets the switch (depends on T010)
- [ ] T016 [US4] Write `specs/DEV-312/quickstart.md` with the exact local regeneration command referenced from ADR-0007 (depends on T015)

**Checkpoint**: Every deliverable exists; regeneration is documented and CI-safe.

---

## Phase 5: Controlled Drift Negative Proof (US2)

**Goal**: Prove the test blocks silent drift.

- [ ] T017 [US2] Mutate one contract field in the generated document, run the drift test, and confirm failure with no baseline writes and no auto-accept; then restore (depends on T010)

**Checkpoint**: Silent drift demonstrably fails the build.

---

## Phase 6: Gates (Gauge-owned; Phase B)

**Purpose**: Phase B gate receipts; not run in Phase A.

- [ ] T018 Report build, tests, format, the three analyzer gates for changed C#, configured property/security/diff gates, and applicable web checks; mutation N/A only while the `harness.yml:17-21` exclusion is live-valid, otherwise the real verdict; preserve the pre-existing `harness.yml:45` change untouched

---

## Dependencies & Execution Order

- **Phase 1**: No dependencies — starts immediately; BLOCKS all later phases.
- **Phase 2**: Depends on Phase 1 (composition must exist before host proof and export).
- **Phase 3**: Depends on Phase 2 (test/host wiring must exist before generating the authoritative baseline).
- **Phase 4**: T014 (Keel) may proceed in parallel with Phases 2–3; T015–T016 depend on Phase 3.
- **Phase 5**: Depends on Phase 3.
- **Phase 6**: Depends on all implementation phases; Gauge-owned.

### Parallel Opportunities

- T001 and T004 (different files, no dependencies) can run in parallel.
- T012 and T013 (different files, conditional) can run in parallel.
- T014 (Keel ADR) runs in parallel with any implementation phase.
