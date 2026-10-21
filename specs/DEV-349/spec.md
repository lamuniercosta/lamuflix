# Feature Specification: DEV-349 Ollama provisioning and isolated real-model proof

**Feature Branch**: `feature/349-spec`

**Created**: 2026-10-21

**Status**: Draft

**Input**: Ticket DEV-349 (size L, parent DEV-288, no ui tag); settled grill brief `specs/DEV-349/brief.md` (Q1–Q8 closed; Plan-challenge L1/L2 amendment); facts from notes `recon-DEV-349` and `recon-DEV-349-2`; Patron rulings in `specs/DEV-349/CONCLUSIONS.md`; taste receipt `specs/DEV-349/ASSUMPTIONS.md`; `specs/PRODUCT.md`; proposed ADR `docs/adr/DEV-349-isolated-ollama-test.md` (unchanged and proposed through Phase A and Gate 1; Quill does not edit or number it now). No decisions beyond `brief.md`.

## Clarifications

### Session 2026-10-21

L-ticket `/speckit-clarify` scan against brief Q1–Q8, CONCLUSIONS, ASSUMPTIONS, and recon notes. No critical ambiguities remain that require a new Patron ruling. Material answers already encoded below from the grill:

- Q: Phase B vs Phase A predecessor readiness → A: Phase A drafts against the documented DEV-412 contract; Phase B entry requires the DEV-412 runtime adapter merged to main and present on the DEV-349 build base; Ready to build is not runtime completion; no placeholder or substitute adapter (brief Q1; recon-DEV-349-2:54-60).
- Q: ADR-0012:15 vs isolated real-model test → A: ADR-0012:15 is default-suite policy; ticket + constitution 526-527 authorize the isolated opt-in exception; proposed ticket ADR records that reconciliation; preserve ADR-0012 (brief Q2).
- Q: How default suites and CI stay Ollama-free → A: `Category=OllamaIntegration` + `LAMUFLIX_RUN_OLLAMA_TESTS=1` checked before any Ollama resource init; CI also excludes the category with no opt-in; opted-in infra failures fail (brief Q3).
- Q: Compose model-ready contract → A: `ollama` + one-shot `ollama-model-init`, shared pinned image and `ollama-models` volume, configurable `all-minilm`, model-aware health; initializer waits on server readiness only (not model-aware healthy) (brief Q4).
- Q: Real-model proof boundary → A: One IntegrationTests test through merged DEV-412 `IEmbeddingGenerator<string, Embedding<float>>` registration/startup seam; assert exactly one embedding of length `EmbeddingDimensions.Value` (384); no new package/project; no fake/raw-client substitute (brief Q5).

Exact Ollama image digest, CLI probe/pull mechanics, cleanup budget number, and merged DEV-412 registration/API names remain Phase B recon facts (brief Q5–Q7), not open product decisions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Provision model-ready Ollama in Compose (Priority: P1)

As the operator, I need `docker compose up` to bring up an Ollama service that finishes healthy only when the server is available and the configured embedding model is present, with a one-shot initializer that pulls the model into persistent development storage on first run and retains it across restart.

**Why this priority**: Ticket AC requires compose up to bring Ollama with the model when health passes (DEV-349:22; brief Authoritative delivery; Q4).

**Independent Test**: Fresh-store Compose evidence shows initializer success and model-aware healthy status; restart retains the model; deliberate readiness/pull failure must not claim healthy completion (brief Q7 acceptance evidence).

**Acceptance Scenarios**:

1. **Given** a clean `ollama-models` volume and Compose file with `ollama` plus `ollama-model-init` sharing one pinned Ollama image, **When** compose starts, **Then** the initializer starts on `service_started`, waits for server readiness with a finite deadline, pulls the configured model (default `all-minilm`), and exits 0 only on success.
2. **Given** that successful initialization, **When** Ollama health is evaluated, **Then** health requires both server availability and selected-model presence (model-aware healthy).
3. **Given** a prior successful pull on the persistent volume, **When** Compose is restarted, **Then** the model remains available without requiring a full re-download for readiness.
4. **Given** a readiness or pull failure in the initializer, **When** compose settles, **Then** the stack must not report model-ready healthy status for that failure path.
5. **Given** existing `postgres`, `rabbitmq`, `api`, and `web` services, **When** Ollama services are added, **Then** those services and their current depends_on/health contracts remain; this ticket does not require API `depends_on` Ollama or public Ollama port exposure (brief Q4; recon-DEV-349:15-20).

---

### User Story 2 - Isolated opt-in real-model 384-dimension proof (Priority: P1)

As the maintainer, I need one opt-in Testcontainers integration test that starts an isolated Ollama with fresh model storage, pulls `all-minilm`, resolves the merged DEV-412 embedding generator through its real registration and validated startup seam, and asserts exactly one 384-dimensional embedding for a fixed sample plot.

**Why this priority**: Ticket AC requires an isolated Testcontainers Ollama test calling the DEV-412 adapter and asserting a 384-dim embedding for a sample plot (DEV-349:22; brief Q5).

**Independent Test**: With `LAMUFLIX_RUN_OLLAMA_TESTS=1`, run the category-filtered command against `LamuFlix.IntegrationTests` and record actual one-vector length `EmbeddingDimensions.Value` (384) plus deterministic disposal on success and failure (brief Test contract; Q7).

**Acceptance Scenarios**:

1. **Given** opt-in is set and Docker is available, **When** `OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding` runs, **Then** it starts the pinned Ollama image with fresh isolated storage and a dynamically mapped endpoint, pulls `all-minilm`, resolves the merged DEV-412 `IEmbeddingGenerator<string, Embedding<float>>` via real registration/startup validation using that endpoint/model, generates the fixed sample plot, and asserts exactly one embedding whose length equals `EmbeddingDimensions.Value` (384).
2. **Given** that test, **When** generation completes, **Then** the asserted output is actual native model vector length, not merely configured metadata or a requested dimensions field (brief Test contract; recon-DEV-349-2:21,25-27).
3. **Given** success or failure of the opted-in path, **When** the test ends, **Then** owned container/model resources are cleaned up on all exits with a separately enforced finite cleanup deadline and failure diagnostics (mechanics from Phase B recon; no invented API or numeric budget).
4. **Given** opted-in Docker, model, readiness, or generation failures, **When** the test observes them, **Then** the test fails (not skips) with actionable diagnostics (brief Q3, Q7).
5. **Given** this ticket's scope, **When** the proof is written, **Then** it uses existing generic Testcontainers primitives already available through existing test dependencies, adds no new NuGet package or test project, and does not use the fake generator, a raw-client substitute, a golden vector, or any database/pgvector path (brief Q5; recon-DEV-349:27,32-34).

---

### User Story 3 - Keep default suites and CI free of Ollama startup (Priority: P1)

As the maintainer, I need ordinary unfiltered `dotnet test` and default CI to skip the real-model test before any Ollama fixture, container, or model initialization, while documenting the exact opt-in invocation.

**Why this priority**: Ticket AC requires that default CI does not start Ollama (DEV-349:22; constitution 526-527; brief Q3).

**Independent Test**: Unfiltered default-suite and CI-filter evidence show no Ollama startup/download; README documents the opt-in command (brief Q7).

**Acceptance Scenarios**:

1. **Given** `LAMUFLIX_RUN_OLLAMA_TESTS` is unset or not `1`, **When** unfiltered `dotnet test` discovers or executes `Category=OllamaIntegration`, **Then** the real-model test skips with no Ollama initialization, including no eager fixture construction/startup path that creates container, model, or resource objects before the opt-in gate.
2. **Given** default CI Test step, **When** it runs, **Then** it uses `--filter "Category!=OllamaIntegration"` (or equivalent exclusion of that category) and sets no opt-in variable (brief Q3; recon-DEV-349:30).
3. **Given** a developer who wants the real-model proof, **When** they follow README, **Then** the documented PowerShell opt-in targets `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` with `--filter "Category=OllamaIntegration"` and clears the env var afterward (brief Test contract).
4. **Given** shared Postgres/RabbitMQ fixtures, **When** isolation is implemented, **Then** gating stays test-local in `OllamaEmbeddingIntegrationTests` / `OllamaFixture` and those shared fixtures are not modified to start Ollama (brief Q3; recon-DEV-349:33).

### Edge Cases

- Phase B must not start until the DEV-412 runtime adapter is merged to main and present on the DEV-349 build base; Ready to build alone is insufficient (brief Q1; recon-DEV-349-2:54-60).
- No nomic substitution, truncation, padding, or dimensions override to manufacture 384 values (brief Q4; recon-DEV-349-2:13-21).
- Do not change `EmbeddingDimensions.Validate` or mask DEV-412 null-metadata/probe behavior in this ticket (brief Test contract; recon-DEV-349-2:21,25-27,64).
- Initializer must not depend on model-aware `service_healthy` before pulling (avoids a health dependency cycle) (brief Q4).
- Test storage must not reuse the developer `ollama-models` volume (brief Q4, Q6).
- No schema/database/pgvector/metadata pipeline, API endpoint/DTO/status, production adapter, host wiring, new package/project/layer, hosted embedding API, LocalPlay, secrets, `Process.Start`, or generated OpenAPI/TS contract edits (brief Authoritative delivery; `specs/PRODUCT.md` §5).
- PRODUCT Epic 9 sequencing is binding: Recommendations / Ollama / pgvector work waits until Epic 7 (SPA) completes (`specs/PRODUCT.md:17`; brief.md:7 makes PRODUCT governing; Q1–Q8 grant no Epic 7 exception). Phase B entry must confirm Epic 7 completion evidence in the bounded prerequisite recon; missing evidence stops for recon. This ticket's delivery envelope remains Compose provisioning plus the isolated proof and does not start the recommendations pipeline; that scope boundary is not a sequencing exception.
- Scope-empty gate exit 2 is `SKIPPED (scope-empty)`, non-blocking and never PASS; Exit 1 and Could not run remain blocking (brief Gate expectations). No property-test opt-out is granted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Add Compose service `ollama` and one-shot service `ollama-model-init` to `docker-compose.yml`, sharing one pinned Ollama image digest and persistent volume `ollama-models`. Preserve existing services and configuration (brief Q4, Q6; recon-DEV-349:13-20).
- **FR-002**: Implement `docker/ollama-init.sh` with finite server wait, model pull of the configured model (default `all-minilm`), explicit failure, and completion status. Pass the configured model as one fixed quoted CLI argument; reject unset or empty model values; do not use eval or code interpolation of the model string. Initializer starts on `service_started` and must not wait on model-aware healthy before pulling (brief Q4, Q6; plan-challenge S2).
- **FR-003**: Ollama health MUST require server availability AND selected-model presence. Failure of readiness/pull MUST NOT yield model-ready healthy status (brief Q4).
- **FR-004**: Model identifier is configurable and defaults to `all-minilm` per DEV-412. No nomic substitution, truncation, padding, or dimensions override (brief Q4; recon-DEV-349-2:13-21).
- **FR-005**: Document non-secret Ollama image/model and operational configuration in `.env.example` only (brief Q6; recon-DEV-349:19).
- **FR-006**: Add `tests/LamuFlix.IntegrationTests/OllamaFixture.cs` and `OllamaEmbeddingIntegrationTests.cs` with trait `Category=OllamaIntegration`, method `GenerateAsync_SamplePlot_Returns384DimensionalEmbedding`, and opt-in gate on `LAMUFLIX_RUN_OLLAMA_TESTS=1` at the earliest fixture construction/startup path before any container, model, or resource object is created, or fully lazy creation behind that gate (brief Q3, Q5, Q6; ASSUMPTIONS; plan-challenge S1).
- **FR-007**: Fixed sample plot text MUST be exactly: `A botanist follows a signal from an abandoned observatory and discovers a hidden garden.` (ASSUMPTIONS; brief Test contract).
- **FR-008**: Opted-in test MUST resolve the merged DEV-412 `IEmbeddingGenerator<string, Embedding<float>>` through its real registration and validated startup seam against the test endpoint/model, generate the sample plot, and assert exactly one embedding with length `EmbeddingDimensions.Value` (384). No fake, raw-client substitute, golden vector, or database (brief Q5; recon-DEV-349:50-55).
- **FR-009**: Default unfiltered `dotnet test` MUST skip the OllamaIntegration test with no Ollama initialization on discovery/execution, including no eager fixture construction/startup path that creates container/model/resource objects before the opt-in gate. After opt-in, Docker/model/readiness/generation errors MUST fail the test (brief Q3; plan-challenge S1).
- **FR-010**: Edit `.github/workflows/ci.yml` Test step to exclude `Category=OllamaIntegration` and set no opt-in variable (AC-forced; brief Q3; recon-DEV-349:30).
- **FR-011**: Update `README.md` with provisioning, health/readiness, first download, settings, failure diagnosis, the exact opt-in invocation from the brief Test contract, and (after Phase B ADR lifecycle) the link to the numbered Accepted ADR path (brief Q6; recon-DEV-349:19-20; brief Plan-challenge amendment).
- **FR-012**: Configurable operational defaults: server readiness 120s, model pull 900s, generation 120s, polling 2s; cleanup has its own separately enforced finite deadline with failure diagnostics (mechanics from Phase B recon; no invented API or numeric budget). Same verified image digest across Compose, initializer, and test; exact pin is a Phase B recon fact (brief Q6; ASSUMPTIONS; plan-challenge S3).
- **FR-013**: No new NuGet/npm dependency, project, top-level folder, architectural layer, shared Postgres/RabbitMQ fixture edit, schema/migration, public API/OpenAPI/web contract, production adapter/host wiring, `Features:LocalPlay`, secrets, or `Process.Start` unless a new bounded Phase B blocker yields a Patron ruling (brief Q5–Q6; `specs/PRODUCT.md` §5; recon-DEV-349:64-70). Phase B ADR rename/Accepted lifecycle for the ticket ADR is authorized by the Plan-challenge amendment and is not a host-wiring expansion.
- **FR-014**: Phase B gate bar follows brief Gate expectations: applicable Roslyn, complexity, InspectCode, property, vulnerability, format, and `dotnet test`. No web changes planned (web gate scope-empty). No production C# under `src/` planned (mutation `SKIPPED (scope-empty)` only when the script returns that verdict). Existing property suite runs normally. Thresholds stay in `harness.yml` and are never lowered. Exit 1 and Could not run block; scope-empty exit 2 is never PASS.
- **FR-015**: Delivery scope is the frozen brief scope: configurable all-minilm Ollama Compose service and model initializer with model-aware health; one isolated opt-in real-model Testcontainers 384-dimensional check through the merged DEV-412 adapter; default-suite/CI isolation and the Q6 support-file envelope as amended for Phase B ADR lifecycle. Scope statements describe that boundary and do not preclude findings about failures to meet in-scope requirements (brief Frozen scope / Closing bar; brief.md:89-97).
- **FR-016**: Keep `docs/adr/DEV-349-isolated-ollama-test.md` unchanged and proposed through Phase A and the Gate 1 spec PR. Quill does not edit or number the ADR now. After Gate 1, Wisp T001 inventories `docs/adr/` on the Phase B `origin/main` base and records the next sequential `ADR-00NN` in recon-DEV-349. Anvil then renames the ticket ADR to `docs/adr/ADR-00NN-isolated-ollama-test.md`, changes status to Accepted, removes only the Phase-A-only proposed-until-Gate-1 disclaimer, and preserves approved Q1–Q2 decision text without re-authoring. No number is chosen in Phase A. T003/T015 authorize this lifecycle change; T013 updates the README link; T015 verifies the numbered Accepted file, old path removed by rename, and ADR-0012 untouched (brief Q2; brief.md:89-97; CONCLUSIONS Plan-challenge L1; constitution.md:499-501,505).

### Key Entities

- **Ollama Compose service**: Development-time Ollama container with model-aware health and persistent model volume.
- **ollama-model-init**: One-shot initializer that waits for server readiness, pulls the configured model, and exits nonzero on failure.
- **OllamaIntegration test**: Opt-in isolated real-model proof through the DEV-412 embedding generator seam.
- **EmbeddingDimensions.Value**: Canonical 384-dimension contract constant used by the length assertion (`recon-DEV-349:52`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Fresh-store Compose provisioning reaches model-aware healthy status after a successful initializer pull of the configured default model.
- **SC-002**: After a successful pull, Compose restart retains model readiness without treating absence of the model as healthy.
- **SC-003**: Controlled readiness/pull failure does not report model-ready healthy completion.
- **SC-004**: Unfiltered default `dotnet test` skips the OllamaIntegration test without starting Ollama or downloading a model.
- **SC-005**: Default CI excludes `Category=OllamaIntegration` and does not set the opt-in variable.
- **SC-006**: Explicit opt-in run returns exactly one embedding of length 384 for the fixed sample plot through the merged DEV-412 adapter and disposes owned resources.
- **SC-007**: Opted-in infrastructure failure fails the test; skip occurs only when opt-in is absent.
- **SC-008**: Phase B gates record honest script-owned verdicts; SKIPPED/SKIP/N/A are never recorded as PASS.

## Assumptions

- Category name `OllamaIntegration` and opt-in `LAMUFLIX_RUN_OLLAMA_TESTS=1` (ASSUMPTIONS.md; tagged `[assumed]`).
- Initializer service name `ollama-model-init`; volume name `ollama-models` (ASSUMPTIONS.md; tagged `[assumed]`).
- Operational budgets 120s / 900s / 120s / 2s plus finite cleanup allowance (ASSUMPTIONS.md; tagged `[assumed]`).
- Sample plot and test method name as in ASSUMPTIONS.md (tagged `[assumed]`).
- Exact Ollama image digest, CLI mechanics, cleanup numeric budget, and merged DEV-412 registration names are Phase B recon facts (brief Q5–Q7).
- PRODUCT Epic 9 sequencing (`specs/PRODUCT.md:17`) remains in force with no Epic 7 exception. Phase B may start Compose provisioning and the isolated proof only after Epic 7 completion evidence is recorded at entry; missing evidence stops for recon. Delivery scope does not authorize recommendations ingest/query work and does not waive the Epic 7 prerequisite.
- No owner structural checkbox is opened by this draft (brief Q8; CONCLUSIONS Q1–Q8).
- Exclusions in brief Frozen scope remain out of scope.
