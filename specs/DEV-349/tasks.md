# Tasks: DEV-349 Ollama provisioning and isolated real-model proof

**Input**: Design documents from `specs/DEV-349/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md), [ASSUMPTIONS.md](ASSUMPTIONS.md)

**Tests**: Required for US2 (the real-model proof is the ticket deliverable). Isolation skip behavior for US3 is verified with default-suite evidence. No red-first requirement beyond what the brief states.

**Organization**: Setup and foundational tasks carry no story label. US1–US3 carry story labels. Polish carries no story label.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story from `spec.md`
- File paths are relative to the worktree `F:\Dev\LamuFlix.worktrees\feature-349-spec`

## Phase 1: Setup (prerequisite and drift recon)

**Purpose**: Confirm Phase B entry prerequisites and re-pin facts before any production edit.

- [X] T001 Wisp bounded Phase B prerequisite/drift recon on the DEV-349 build base (`origin/main` after Gate 1): confirm Epic 7 (SPA) completion evidence required by PRODUCT Epic 9 sequencing (`specs/PRODUCT.md:17`; brief.md:7; Q1–Q8 grant no Epic 7 exception) without asserting current completeness in this task text; confirm merged DEV-412 runtime adapter on main and present on this base; record actual registration/startup API names; confirm generic Testcontainers container API availability through existing references without a new package; select and record the Ollama image version/digest shared by Compose, initializer, and test; record available image CLI/probe/pull mechanics and verified finite cleanup mechanics that support ownership cleanup on all exits with a separately enforceable deadline and failure diagnostics (do not invent a disposal API or numeric budget); inventory `docs/adr/` and record the next sequential `ADR-00NN` number in recon-DEV-349 (no guessed number in Phase A); reconcile DEV-325 Compose and current CI/test files against recon-DEV-349 / recon-DEV-349-2. Recorded Ready-to-build / spec handoff alone is insufficient (brief Q1, Q7; brief Plan-challenge amendment). Missing Epic 7 or DEV-412 evidence stops to recon; off-envelope changes stop to Patron. Do not invent registration/class names, pins, package additions, Epic 7 status claims, or compatibility claims.
- [X] T002 Confirm T001 evidence satisfies Phase B entry: PRODUCT Epic 7 completion evidence is present (`specs/PRODUCT.md:17`) and brief Q1 predecessor is satisfied (DEV-412 runtime merged and present). If either is not satisfied, stop and report the blocker; do not start Compose or test edits. Compose plus isolated proof is the delivery envelope, not a sequencing exception.

**Checkpoint**: Epic 7 sequencing, predecessor, and drift facts are current. Story edits may begin only after T002.

---

## Phase 2: Foundational (file envelope hold)

**Purpose**: Hold edits inside the approved envelope.

- [X] T003 Start story edits only after T002. Allowed modify set: `docker-compose.yml`, `.github/workflows/ci.yml`, `.env.example`, `README.md`. Allowed add set: `docker/ollama-init.sh`, `tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs`, `tests/LamuFlix.IntegrationTests/OllamaFixture.cs`. Phase B ADR lifecycle (authorized by brief Plan-challenge amendment): after T001 records the next sequential number, Anvil renames (`git mv`) `docs/adr/DEV-349-isolated-ollama-test.md` to `docs/adr/ADR-00NN-isolated-ollama-test.md`, changes status to Accepted, removes only the Phase-A-only proposed-until-Gate-1 disclaimer, and preserves approved Q1–Q2 decision text without re-authoring. Do not edit csproj/package/shared Postgres/RabbitMQ fixtures, `src/` production adapter/host wiring, `EmbeddingDimensions.Validate`, or `docs/adr/ADR-0012.md`. Do not edit, number, or Accept the proposed ADR in Phase A.

**Checkpoint**: Story work stays inside the file boundary.

---

## Phase 3: User Story 1 - Provision model-ready Ollama in Compose (Priority: P1)

**Goal**: Compose brings Ollama with configurable `all-minilm`, model-aware health, and a one-shot initializer on a persistent volume.

**Independent Test**: Fresh-store compose up shows initializer success and model-aware healthy; restart retains the model; controlled failure is not healthy.

- [X] T004 [US1] Add `ollama` service and persistent volume `ollama-models` to `docker-compose.yml` using the T001 image digest pin. Configure model-aware health requiring server availability and selected-model presence. Preserve existing `postgres`, `rabbitmq`, `api`, and `web` services and their depends_on/health contracts. Do not add API `depends_on` Ollama or require public Ollama port exposure.
- [X] T005 [US1] Add one-shot `ollama-model-init` service to `docker-compose.yml` sharing the same image pin and `ollama-models` volume. Start on `service_started` (not model-aware `service_healthy`). Wire configurable model defaulting to `all-minilm` and the Q6 operational budgets from ASSUMPTIONS.
- [X] T006 [US1] Add `docker/ollama-init.sh` implementing finite server wait, model pull, explicit failure, and completion status using T001 CLI/probe/pull mechanics. Pass the configured model as one fixed quoted CLI argument; reject unset or empty model values; do not use eval or code interpolation of the model string. Exit 0 only on success. Do not wait on model-aware healthy before pulling. Preserve configurability and the existing file envelope.
- [X] T007 [P] [US1] Document non-secret Ollama image/model and operational configuration in `.env.example` without adding secret literals.
- [X] T008 [US1] Collect fresh-store Compose evidence: initializer success and model-aware healthy; restart retention; deliberate readiness/pull failure does not claim healthy completion. Record commands and outcomes for Gauge.

**Checkpoint**: US1 provisioning contract is met. US2/US3 may proceed.

---

## Phase 4: User Story 2 - Isolated opt-in real-model 384-dimension proof (Priority: P1)

**Goal**: One IntegrationTests opt-in proof through the merged DEV-412 seam asserting a single 384-dimensional embedding.

**Independent Test**: Opt-in category-filtered run returns one vector of length `EmbeddingDimensions.Value` and disposes resources.

- [ ] T009 [US2] Add `tests/LamuFlix.IntegrationTests/OllamaFixture.cs` with test-local isolated container/model/startup/cleanup support using existing generic Testcontainers primitives and the T001 image digest. Use fresh storage that does not reuse `ollama-models`. Apply finite operation budgets from ASSUMPTIONS / T001. Place the opt-in check at the earliest fixture construction/startup path before any container, model, or resource object is created, or use fully lazy creation gated behind that check. Connect T001-verified finite cleanup mechanics to ownership cleanup on all exits, with a separately enforced cleanup deadline and failure diagnostics; do not invent an API or numeric budget.
- [ ] T010 [US2] Add `tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs` with `[Trait("Category", "OllamaIntegration")]` and method `GenerateAsync_SamplePlot_Returns384DimensionalEmbedding`. Gate on `LAMUFLIX_RUN_OLLAMA_TESTS=1` at the earliest fixture construction/startup path before any container/model/resource object is created (or require fully lazy gated creation); a test-body gate may remain as defence in depth. Resolve the merged DEV-412 `IEmbeddingGenerator<string, Embedding<float>>` through its real registration/validated startup seam using the test endpoint/model. Generate the fixed sample plot from ASSUMPTIONS. Assert exactly one embedding with length `EmbeddingDimensions.Value` (384). Ownership cleanup on success and failure using T001-verified mechanics with a separately enforced cleanup deadline and failure diagnostics. No fake, raw-client substitute, golden vector, or database.
- [ ] T011 [US2] Run the brief opt-in command against `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` with `--filter "Category=OllamaIntegration"` and record actual one-vector 384 output plus explicit success and failure cleanup evidence under the separately enforced cleanup deadline, including failure diagnostics when cleanup cannot complete. Opted-in infrastructure failure must fail (not skip). If the explicit run cannot execute, stop — required runtime proof is missing.

**Checkpoint**: US2 proof contract is met.

---

## Phase 5: User Story 3 - Default suite and CI isolation (Priority: P1)

**Goal**: Default unfiltered tests and CI never start Ollama; README documents the opt-in path.

**Independent Test**: Unfiltered default suite and CI-filter evidence show no Ollama startup/download; README contains the exact opt-in command.

- [ ] T012 [P] [US3] Edit `.github/workflows/ci.yml` Test step to exclude `Category=OllamaIntegration` (for example `--filter "Category!=OllamaIntegration"`) and set no `LAMUFLIX_RUN_OLLAMA_TESTS` variable.
- [ ] T013 [P] [US3] Update `README.md` with provisioning, health/readiness, first download, settings, failure diagnosis, the exact opt-in invocation from the brief Test contract, and the ADR link to the T001-verified numbered Accepted path after the Phase B rename (`constitution.md:505`). Preserve unrelated README content.
- [ ] T014 [US3] Collect unfiltered default `dotnet test` evidence showing the OllamaIntegration test skips with no Ollama initialization on default unfiltered discovery/execution, including that eager fixture construction/startup paths do not create container/model/resource objects before the opt-in gate; and CI-filter evidence showing category exclusion without opt-in. Record outcomes for Gauge.

**Checkpoint**: US3 isolation contract is met. Ticket AC have provisioning, proof, and isolation evidence paths.

---

## Phase 6: Polish and close-out

**Purpose**: Boundary check, applicable gates, and the three review axes.

- [ ] T015 Confirm the implementation diff stays inside the T003 envelope. Confirm the numbered `docs/adr/ADR-00NN-isolated-ollama-test.md` file exists with status Accepted, the old path `docs/adr/DEV-349-isolated-ollama-test.md` is removed by the rename, and `docs/adr/ADR-0012.md` is untouched. Confirm no `src/` production C# was added unless a new Patron ruling authorized it. Confirm shared Postgres/RabbitMQ fixtures are unmodified.
- [ ] T016 Gauge runs applicable gates with honest script-owned verdicts: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, `dotnet format --verify-no-changes`, `./scripts/run-vulnerable-packages.ps1`, `./scripts/run-property-tests.ps1` (existing suite; no new property invariant), `dotnet test`, pipeline web gate, and mutation when applicable. Record each exit. Exit 1 and Could not run block. Scope-empty exit 2 is `SKIPPED (scope-empty)`, never PASS. Mutation `SKIPPED (scope-empty)` only when the script returns that verdict for no production C# under `src/`. Do not edit `harness.yml` or lower thresholds.
- [ ] T017 Sentry, Ledger, and Compass review the implementation diff against the closing bar and frozen scope in `brief.md`. Critical/High/Medium in-scope findings block. Low findings receive charter-compliant dispositions. Maximum two review/remediation rounds. Unresolved above-bar findings at the cap stop and report the blocker. This task does not merge.

**Checkpoint**: Gates and the three axes are recorded. Later-head gates after rebase remain required and are not waived.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: starts at Phase B entry. T002 follows T001 and must clear both Epic 7 completion evidence (`specs/PRODUCT.md:17`) and DEV-412 predecessor evidence before any story edit.
- **Foundational (Phase 2)**: follows T002. Blocks every story edit.
- **US1 (Phase 3)**: follows T003. T004 → T005 → T006; T007 may run beside T004–T006; T008 follows T004–T006.
- **US2 (Phase 4)**: follows T008 (Compose pin/mechanics stable) and T002 (adapter present). T009 → T010 → T011.
- **US3 (Phase 5)**: follows T010 (trait/opt-in exist to exclude/document). T012 and T013 may run together; T014 follows T012 and T010.
- **Polish (Phase 6)**: follows T008, T011, and T014. T016 follows T015. T017 follows T016.

### User Story Dependencies

- **US1 (P1)**: Compose provisioning. Supplies the shared image pin and model defaults used by US2; can be validated with Compose evidence before the opt-in test.
- **US2 (P1)**: Real-model proof. Depends on T001/T002 predecessor and US1 pin/mechanics.
- **US3 (P1)**: Isolation and docs. Depends on the trait/opt-in from US2 for meaningful CI exclusion and README command accuracy; CI filter task may be prepared once the category name is fixed (ASSUMPTIONS).

### Parallel Opportunities

- T007 beside T004–T006 after T003.
- T012 and T013 beside each other after T010.
- No parallel story implementation that edits the same file.

### Parallel Example: US3 docs and CI

```text
T012 .github/workflows/ci.yml category exclusion
T013 README.md provisioning and opt-in command
```

---

## Implementation Strategy

### Ticket delivery

Acceptance criteria are met when US1–US3 evidence exists: model-ready Compose, opt-in 384-dim real-adapter proof, and default-suite/CI isolation. US1 lands before the opted-in proof run. US3 CI/docs land with or immediately after the trait exists.

### Incremental checkpoints

1. Finish Phase 1–2 before editing production files.
2. Finish US1 and collect Compose evidence.
3. Finish US2 opt-in proof and record 384-dim output + cleanup.
4. Finish US3 isolation evidence and docs.
5. Boundary check, gates, three review axes.

### Close-out

Commit messages during implementation start `DEV-349 - `. This task list does not authorize a merge. Optional Gherkin is not generated. Quill does not author, number, or Accept ADRs in Phase A; the proposed ADR stays unchanged through Gate 1, and Phase B Anvil performs the authorized rename/Accepted lifecycle after T001. No glossary / CONTEXT.md edit.

## Notes

- [P] tasks = different files, no incomplete dependency
- [Story] label maps task to US1–US3
- Exact image digest, CLI mechanics, registration API names, cleanup numeric budget, cleanup enforcement mechanics, and next ADR-00NN come from T001 — never invent them in an earlier phase
- Do not change `EmbeddingDimensions.Validate` or mask DEV-412 null-metadata/probe behavior
- Anything outside the frozen scope is a follow-up issue, not a finding in this round (brief Frozen scope)
