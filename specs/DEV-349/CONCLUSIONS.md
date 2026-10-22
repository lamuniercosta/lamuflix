# DEV-349 grill conclusions

Append-only full exchanges. Decision owner: Patron. Recorder: Keel. Phase A only.

---

## Q1/12 - Predecessor readiness

### Keel question

[from Keel] /grill-with-docs DEV-349 Q1/12. Worktree F:/Dev/LamuFlix.worktrees/feature-349-spec branch feature/349-spec. Please act as decision owner, one question at a time; return a cited ACCEPT/AMEND/blocked ruling and report back to Keel. Keel will append the full exchange before proceeding; please do not write files, commit, run recon, or contact Rigger during this grill. Basis: DEV-349:22; recon-DEV-349-2:54-60 separates spec handoff from runtime availability. I recommend Phase A may close against the documented DEV-412 contract, but Phase B entry requires DEV-412 runtime adapter merged into main and present on the DEV-349 build base; no staged placeholder test or substitute adapter. Cost: DEV-349 implementation waits for the predecessor, keeping compose and the real-model test in one complete delivery. Do you approve this dependency boundary? Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q1/12 ACCEPT: Phase A closes against the documented DEV-412 contract (recon-DEV-349-2:11-35,58), while Phase B entry requires the DEV-412 runtime adapter merged to main and present on the DEV-349 build base with no staged placeholder test or substitute adapter (DEV-349:22; recon-DEV-349-2:54-60).

### Rationale and implications

Ready to build is spec handoff, not runtime completion. Planning may proceed using the cited contract; Phase B waits for the actual merged adapter. No substitute adapter or placeholder delivery.

---

## Q2/12 - Default-suite policy and isolated exception

### Keel question

[from Keel] DEV-349 Q2/12. Basis: recon-DEV-349:70 quotes constitution 526-527 allowing one isolated Ollama category but ADR-0012:15 says no test calls Ollama; DEV-349:22 explicitly requires the real-model test. I recommend read the ADR prohibition as the default test suite policy, with this ticket and constitution supplying the explicit isolated opt-in exception; preserve deterministic fake defaults, no hosted APIs, no constitution departure or ticket reduction. Record this limited clarification in a DEV-349 ADR draft at docs/adr/DEV-349-isolated-ollama-test.md (ticket-specific name avoids inventing a reserved ADR number; proposed until Gate 1). Do you approve this reconciliation and ADR scope, or is an owner checkbox required? Cost: explicit opt-in maintenance, never silent default Ollama traffic. No file writes by Patron. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q2/12 ACCEPT: No owner checkbox is required because both the ticket and constitution explicitly authorize the isolated opt-in Ollama Testcontainers test category outside default CI (DEV-349:22; .specify/memory/constitution.md:526-527; recon-DEV-349:72). Reconcile docs/adr/ADR-0012.md:15 as the default test-suite policy (deterministic fake by default, no hosted APIs) and record the isolated opt-in exception in docs/adr/DEV-349-isolated-ollama-test.md as proposed until Gate 1.

### Rationale and implications

The constitution and ticket authorize the isolated exception. Default tests remain fake-only; no hosted embedding API is introduced. Record a proposed ticket-specific ADR without rewriting ADR-0012.

---

## Q3/12 - Default-suite isolation

### Keel question

[from Keel] DEV-349 Q3/12. Basis: recon-DEV-349:24-33 finds only Category=Property and default CI runs unfiltered dotnet test; ticket DEV-349:22 requires default CI never start Ollama. I recommend Category=OllamaIntegration plus explicit process opt-in LAMUFLIX_RUN_OLLAMA_TESTS=1, checked before any Ollama fixture/container/model initialization. Ordinary dotnet test must skip the real-model test without starting Ollama even outside CI; default CI also excludes that category explicitly, with no opt-in variable. Document opt-in command scoped to the category and failure on Docker/model/startup errors once opted in. Add test-local gating, do not modify shared Postgres/RabbitMQ fixtures; no default test may accidentally start Ollama. Do you approve this isolation contract and the AC-forced .github/workflows/ci.yml edit? Cost: two safeguards and explicit manual execution; no automatic model downloads in default checks. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q3/12 ACCEPT: Approve Category=OllamaIntegration plus explicit process opt-in LAMUFLIX_RUN_OLLAMA_TESTS=1 checked before any Ollama fixture/container/model initialization so ordinary unfiltered dotnet test skips without starting Ollama (.specify/memory/constitution.md:526-527; recon-DEV-349:28-33; log category and env var names [assumed] in ASSUMPTIONS.md). Approve the edit to .github/workflows/ci.yml:31-32 to filter out Category=OllamaIntegration as an AC-forced edit under Charter 2.3 item 6 (DEV-349:22 AC: default CI does not start Ollama; recon-DEV-349:30), keeping gating test-local without modifying shared Postgres/RabbitMQ fixtures (recon-DEV-349:33).

### Rationale and implications

Explicit opt-in is checked before resource initialization, so unfiltered local tests remain safe as well as filtered CI. Opted-in infrastructure failures fail the test rather than silently skipping. Names are taste assumptions; the isolation requirement is authoritative.

---

## Q4/12 - Model-ready Compose provisioning

### Keel question

[from Keel] DEV-349 Q4/12. Basis: DEV-349:22; recon-DEV-349:13-20; recon-DEV-349-2:13-21,33-35. I recommend Compose adds ollama and a one-shot model initializer using the same pinned Ollama image and shared persistent model volume. Use configurable model default all-minilm matching DEV-412; no nomic substitution, truncation, padding, or dimensions override. Ollama service health must require server availability AND selected-model presence. Initializer starts on service_started, independently waits for server readiness with a finite deadline, pulls the model, exits 0 only on success; never wait on model-dependent service_healthy before pulling (avoids a cycle). Pull/readiness failures are visible and cannot yield healthy model-ready status. Restart preserves the model; test uses a fresh isolated model store. No API host wiring, public port exposure requirement, or API depends_on change in this ticket. Do you approve this provisioning boundary? Cost: one initializer service and first-run download; initialization internals/image pin and timeout mechanics must be verified during bounded Phase B recon, with no invented compatibility claim here. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q4/12 ACCEPT: Approve the compose provisioning boundary: ollama service plus one-shot model initializer sharing the pinned Ollama image and persistent model volume, defaulting to configurable all-minilm per DEV-412 with no nomic substitution, truncation, padding, or dimensions override (DEV-349:22; recon-DEV-349-2:13-21). Approve requiring both server availability and selected-model presence for ollama health while the initializer starts on service_started with a finite readiness deadline, pulls the model, and exits 0 only on success (avoiding healthcheck deadlock), with fresh isolated model store in tests and no API host wiring, public port exposure, or API depends_on change in DEV-349 (recon-DEV-349:15-20; recon-DEV-349-2:33-35; log initializer service/volume naming and timeout defaults [assumed] in ASSUMPTIONS.md).

### Rationale and implications

Model-aware health prevents server-only readiness from claiming provisioning is complete. The initializer waits for server availability independently, avoiding a dependency cycle. Persistent development storage and fresh test storage serve different purposes. Image compatibility and exact command mechanics remain Phase B recon facts, not observed behavior.

---

## Q5/12 - Real-adapter proof boundary

### Keel question

[from Keel] DEV-349 Q5/12. Basis: DEV-349:22; recon-DEV-349:25-33,50-58; recon-DEV-349-2:31-42,59-60. I recommend one real-model test in existing LamuFlix.IntegrationTests, using test-local Testcontainers generic primitives available through existing test dependencies, no new Ollama module/package/project. Start an isolated pinned-image Ollama, dynamically map its endpoint, pull all-minilm to fresh storage, then resolve the actual merged DEV-412 IEmbeddingGenerator<string, Embedding<float>> through its real registration/validated startup seam with that endpoint/model and generate a fixed nonempty sample plot. Require exactly one embedding with length EmbeddingDimensions.Value (384); no golden vector, no fake/raw-client substitute, no DB/pgvector/metadata pipeline. Dispose all owned resources even on failure. Missing registration APIs or generic container availability trigger bounded recon/decision before implementation, never guessed names or dependency changes. Do you approve this proof boundary? Cost: explicit execution downloads a model and waits for real startup; adapter WireMock/error-mapping tests remain DEV-412. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q5/12 ACCEPT: Approve placing the single real-model test in existing LamuFlix.IntegrationTests using generic Testcontainers primitives already available via existing test dependencies, adding no new NuGet package or test project under Charter 2.3 items 1-2 (recon-DEV-349:27,32-34,66-67). Approve starting an isolated pinned-image Ollama container with fresh storage and dynamically mapped endpoint, pulling all-minilm, resolving the merged DEV-412 IEmbeddingGenerator<string, Embedding<float>> through its real registration/startup validation seam, and asserting a single embedding of length EmbeddingDimensions.Value (384) for a fixed non-empty sample plot with deterministic resource disposal and no golden vector, fake/raw-client substitute, or DB/pgvector/metadata scope (DEV-349:22; recon-DEV-349:52-55; recon-DEV-349-2:31-42,59-60; log sample plot text and test class/method naming [assumed] in ASSUMPTIONS.md).

### Rationale and implications

The test exercises the actual DEV-412 seam and actual model output. No substitute adapter or unrelated database behavior may satisfy the assertion. Existing generic container API availability and merged registration names require Phase B confirmation; they are not verified here.

---

## Q6/12 - File envelope and concrete defaults

### Keel question

[from Keel] DEV-349 Q6/12. I recommend this concrete planning envelope: modify docker-compose.yml, .github/workflows/ci.yml, .env.example, README.md; add docker/ollama-init.sh, tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs, and test-local OllamaFixture.cs in that same project. No csproj/package/shared-fixture edits unless bounded recon returns an explicit blocker for Patron. Existing README/env edits document the isolated command, model/image settings, readiness, first download, and failure diagnosis; no unrelated rewrites. Name initializer ollama-model-init and volume ollama-models. Configurable finite default budgets: server wait 120s, model pull 900s, generation 120s, polling 2s; bound fixture lifetime with cleanup beyond these operation budgets. Use the same verified image version/digest for Compose/initializer/test (exact pin is a Phase B recon fact, not invented now). Sample plot: A botanist follows a signal from an abandoned observatory and discovers a hidden garden. Class OllamaEmbeddingIntegrationTests; method GenerateAsync_SamplePlot_Returns384DimensionalEmbedding. Add no domain glossary terms: these are infrastructure concepts. Do you approve the envelope and these taste defaults? Cost: several small named support files; record naming, sample plot and timing defaults [assumed]. Basis: recon-DEV-349:13-20,25-33,64-70; prior Q3-Q5. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q6/12 ACCEPT: Approve the file envelope under Charter 2.3 item 6: modify docker-compose.yml (ticket-named, DEV-349:22), .github/workflows/ci.yml (AC-forced, recon-DEV-349:30), .env.example, and README.md (scoped to Ollama compose/model settings, readiness, first-run download, failure diagnosis, and isolated test invocation per existing repo conventions at recon-DEV-349:19-20,71), and add docker/ollama-init.sh, tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs, and tests/LamuFlix.IntegrationTests/OllamaFixture.cs with no csproj/package/shared-fixture edits unless bounded Phase B recon surfaces a blocker. Approve sharing one verified Ollama image digest pin across Compose, initializer, and test (recon-DEV-349:15) and adding no domain glossary terms. Accept and log [assumed] in ASSUMPTIONS.md the taste defaults: ollama-model-init, ollama-models, budgets (120s server wait, 900s pull, 120s generation, 2s poll, plus cleanup allowance), sample plot string, and OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding.

### Rationale and implications

Only named support-file edits are authorized; package, project, shared-fixture, API, schema, and host-registration changes are excluded. Budgets are configurable operational defaults, not changes to harness thresholds. No domain term is introduced; no CONTEXT.md edit is required.

---

## Q7/12 - Task ordering and acceptance evidence

### Keel question

[from Keel] DEV-349 Q7/12. I recommend task ordering: Phase B prerequisite/drift recon confirms merged DEV-412 actual registration/startup seam, generic container API availability, exact image digest/CLI readiness-pull commands and finite cleanup mechanics; stop for missing evidence or scope changes. Then implement model-ready Compose/init, then isolated real-adapter test and CI exclusion/docs, then Gauge verification. Acceptance evidence must include fresh-store compose provisioning (init success and model-aware healthy), restart retention, controlled pull/readiness failure not healthy, unfiltered default test run and default CI filter proving no Ollama/model download, and explicit opt-in real-adapter test returning one 384 vector with cleanup including failure. Opted-in infrastructure failures fail; skip is only absent opt-in. Gauge runs normal applicable backend gates unchanged; no web changes; no production C# planned means mutation reports script-owned SKIPPED (scope-empty), never PASS; property tests remain normal because existing property suite exists. No new domain/property invariants or acceptance framework. Do you approve this proof and ordering contract? Cost: explicit opt-in validation and failure-path evidence beyond normal fake-only tests; no execution during Phase A. Basis: DEV-349:22, recon-DEV-349:26-33,50-58, Q1-Q6 and task-pipeline Phase 3 step 5. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q7/12 ACCEPT: Approve the Phase B task ordering and proof contract: bounded prerequisite/drift recon first (confirming merged DEV-412 registration/startup seam, generic Testcontainers API, exact Ollama image digest, CLI readiness/pull commands, and finite cleanup), then model-ready Compose/init, then isolated real-adapter test plus CI exclusion and docs, then Gauge verification (DEV-349:22; recon-DEV-349:26-33,50-58; recon-DEV-349-2:54-60). Approve the acceptance evidence set (fresh-store compose health, restart retention, controlled pull/readiness failure not healthy, unfiltered default dotnet test and CI filter skipping without starting Ollama, and opted-in real-adapter 384-dim proof failing closed on infra errors with cleanup) and gate reporting (mutation reported as SKIPPED (scope-empty) and never PASS when no src/ C# changes; existing property suite runs normally; no new property invariants or acceptance framework).

### Rationale and implications

Planning records future proof obligations, not executed results. Default-suite isolation and explicit real-model success are separate required observations. Infrastructure failure after opt-in must fail; applicable gates retain their configured thresholds and script-owned verdicts.

---

## Q8/12 - Loop terms and shared understanding

### Keel question

[from Keel] DEV-349 Q8/12 closeout. I recommend closing bar: no confirmed Critical/High/Medium in-scope findings, all ticket AC proven and all applicable configured gates satisfied, with script-owned nonblocking SKIPPED/scope-empty/N/A/opt-out reported accurately; Low dispositions follow charter noted, no ticket unless broken behavior/user-requested or another charter filing condition. Frozen scope: provision the configurable all-minilm Ollama service/model with model-aware health, and one isolated opt-in real-model Testcontainers check through merged DEV-412 returning 384 dimensions, default-suite/CI exclusion and only Q6 support-file envelope; anything else is a follow-up issue, not a finding in this round. This does not automatically authorize filing a tracker issue. Round cap: two correction rounds for drafting/analysis, two plan-challenge correction rounds, and two formal review/remediation rounds per respective later phase; unresolved above-bar findings at the cap stop, never lower gates or run an extra round silently. Keel now records brief.md, append-only full Q&A, [assumed] taste choices and proposed ADR; no new glossary terms, no owner checkbox under Q1-Q7, no Quill artifacts/freeze/Gate 1/code/gates in this ask. Is shared understanding reached on Q1-Q7 and these loop terms, authorizing Phase 2 step 1 closeout? Cost: explicit bounded follow-up rather than expanding scope. Basis: task-pipeline Phase 2, PRODUCT sections 2-5, charter 2.3, DEV-349:22 and recorded Q1-Q7. Facts come from recon-DEV-349 and recon-DEV-349-2. If you need one that is not there, reply needs recon: <question> and stop there.

### Patron answer

DEV-349 Q8/12 ACCEPT: Shared understanding is reached on Q1-Q7 with zero owner structural checkboxes, ratifying the chain standing closing bar, frozen DEV-349 scope (configurable all-minilm Ollama compose service/initializer with model-aware health, one isolated opt-in real-model Testcontainers 384-dim test through merged DEV-412, default-suite/CI exclusion, and Q6 file envelope), and 2-round cap per phase (DEV-349:22; task-pipeline Phase 2; lamuflix-team-charter 2.3; DEV-412:38). Phase 2 step 1 closeout is authorized for Keel to record specs/DEV-349/brief.md, specs/DEV-349/CONCLUSIONS.md, specs/DEV-349/ASSUMPTIONS.md, and docs/adr/DEV-349-isolated-ollama-test.md.

### Rationale and implications

Grill closed at Q8/12. Zero owner structural checkboxes. The brief authorizes subsequent drafting, not implementation, plan freeze, Gate 1, a PR, or merge. Round caps do not waive gates or unresolved findings. New tracker issues still require charter-compliant Patron decisions and Rigger execution.

---

## Plan-challenge L1/L2 - full request and Patron ruling, 2026-10-21

### Keel request (verbatim)

[from Keel] DEV-349 blocked: plan challenge needs cited L1/L2 rulings; stop after routing this ask. Read adjudication-DEV-349-plan-round1 items 4-5 and pending correction list. L1: constitution.md:499-501 requires numbered Accepted ADRs; CONCLUSIONS Q2 and brief.md:18 preserve ticket ADR as proposed until Gate 1, while tasks T003/T015 prohibit edits. Rule compliant lifecycle, owner and transition point without changing ADR now; do not silently defer compliance to implementation PR. L2: constitution.md:253-258 requires registered health check for every new external dependency; DEV-349 owns Compose health and excludes API host wiring (brief.md:11-13,20-21). recon-DEV-349-2:31-35,60,64-65 establishes DEV-412 adapter/startup probe ownership but no registered API health check. Rule VI application and compliance owner; do not assume a startup probe or Compose health equals registered check. Facts only from recon-DEV-349 and recon-DEV-349-2; missing fact means needs recon to Conductor. No new searches or writes, no freeze/Gate 1. Report cited ruling to Keel; full exchange will be recorded before any Quill dispatch. If constitution departure is necessary, route only under PRODUCT section 5. No owner checkbox inferred.

### Patron ruling (verbatim file)

# [from Patron] DEV-349 Plan-Challenge L1 & L2 Rulings (2026-10-21)

## Verdict
ACCEPT L1 and L2 adjudications (`adjudication-DEV-349-plan-round1:12-13,22-23`) with explicit Patron rulings below; zero owner structural checkboxes and no new Phase A recon required.

## L1 Ruling — ADR Lifecycle, Transition Point, and Owners (`constitution.md:499-501,505`)
- **Phase A state (no edit to ADR now)**: Preserve `docs/adr/DEV-349-isolated-ollama-test.md` unchanged at its current path and `status: proposed` through Phase A and the Gate 1 spec PR (`CONCLUSIONS.md` Q2; `brief.md:18`). Gate 1 (user merge of the Phase A spec PR, `task-pipeline:18,83`) is the approval gate, and assigning a fixed `ADR-00NN` number on a spec branch before Phase B intake risks number collisions across concurrent branches.
- **Explicit transition point and owners (post-Gate 1 in Phase B, amending Q6/T003/T015 envelope)**:
  1. **T001 (Wisp, Phase B prerequisite/drift recon)**: On the Phase B build base (`origin/main` after Gate 1 merge), Wisp inventories `docs/adr/` and records the next sequential `ADR-00NN` number in `recon-DEV-349` (no guessed number in Phase A).
  2. **Phase B implementation (Anvil, authorized in `brief.md`, `plan.md`, and `tasks.md` T003/T013/T015)**: Amend the T003/T015 prohibition on `docs/adr/DEV-349-isolated-ollama-test.md` into an explicit lifecycle task: Anvil renames (`git mv`) `docs/adr/DEV-349-isolated-ollama-test.md` to the T001-verified `docs/adr/ADR-00NN-isolated-ollama-test.md`, changes `status: proposed` to `status: Accepted` (removing only the Phase-A-only proposed-until-Gate-1 disclaimer while preserving the approved Q1–Q2 decision text without re-authoring), updates the ADR link in `README.md` (`T013`; `constitution.md:505`), and keeps `docs/adr/ADR-0012.md` unmodified.
  3. **T015 (Phase 6 boundary check)**: Verify the numbered `ADR-00NN` file has `status: Accepted`, `docs/adr/DEV-349-isolated-ollama-test.md` is removed by the rename, and `docs/adr/ADR-0012.md` is untouched. No constitution departure or owner checkbox is required.

## L2 Ruling — Principle VI Application, Compliance Owner, and Numbering (`constitution.md:239-259,550-551`)
- **Compose health and startup probe are not Principle VI `ServiceDefaults` health checks**: Neither DEV-349's Docker Compose `ollama.healthcheck` (`DEV-349:22`; `spec.md` FR-003) nor a startup validation probe (`recon-DEV-349-2:34`) is a registered ASP.NET Core `/health/ready` health check in `ServiceDefaults` (`constitution.md:253-258`). `plan.md` must state this distinction explicitly.
- **Principle VI application to DEV-349 and compliance owner**: Principle VI (`constitution.md:253-258`) requires `ServiceDefaults` health-check registration when an external dependency is wired into the application host (`GET /health/ready`). DEV-349 adds no `src/` production C# code, no `LamuFlix.Api` or `LamuFlix.ServiceDefaults` host wiring, and no `api` `depends_on: ollama` (`DEV-349:22`; `recon-DEV-349:15-20,42,57,69`; `CONCLUSIONS.md` Q4–Q6). Therefore, Principle VI host `/health/ready` compliance is owned by the runtime adapter/host-wiring slice (`DEV-412`, `recon-DEV-349-2:31-35,58-60`), while DEV-349 owns only the container-level Compose `ollama.healthcheck` (`DEV-349:22`) and the test-local Testcontainers proof. Because `specs/DEV-412/` is outside DEV-349's Phase A recon roots (`recon-DEV-349-2:64-65`), `plan.md` must record Principle VI host-check ownership with DEV-412 without asserting unverified `specs/DEV-412/` details, and T001 already verifies the merged DEV-412 registration/startup seam on `origin/main` before Phase B implementation begins (`CONCLUSIONS.md` Q1, Q5, Q7). No new Phase A recon, constitution departure, or owner checkbox is required.
- **Constitution Check numbering (`constitution.md:550-551`)**: Every `plan.md` Constitution Check item must cite its Roman numeral Principle (`I`–`IX`, including an explicit `Principle VI` item); entries referencing unnumbered constitution sections (`Technology Stack` `:346`, `Documentation Rules` `:499-507`, `Known Technical Debt` `:526-527`, `Static-Analysis Gates` `:366-389`) must explicitly cite `Principle number: N/A (supporting constitution section)` alongside the nearest applicable numbered principle.

---

## Phase 3 Step 2 Rulings — T002, F1, ADR Numbering (2026-10-22)

[from Patron] RULING (DEV-349 Phase 3 step 2): (1) T002 CLEARED — proceed to Phase 2 hold and Phase 3 story edits; (2) F1 AUTHORIZED — `docker/ollama-init.sh` is approved inside the existing tracked `docker/` folder; (3) ADR number is 0025 — Anvil renames to `docs/adr/ADR-0025-isolated-ollama-test.md`.

- **T002 (§2.3, `tasks.md` T001/T002)**: DEV-412 is verified on `origin/main` at build base `17a639d`, including `86692f2` adapter and `1ed8fe1` generator (`recon-DEV-349:62-68`); Epic 7 sequencing under `specs/PRODUCT.md:17` is already ruled SATISFIED / APPROVED TO BUILD by standing precedent (`specs/DEV-338/CONCLUSIONS.md` Phase B Intake Ruling (1), `ecfcfb4`; confirmed `specs/DEV-342/CONCLUSIONS.md` Phase B Pickup Ruling (2), `8790957`, owner's word final), so no new Epic 7 completion proof or exception is required for DEV-349.
- **F1 (§2.3 item 2/6)**: `docker/` is not a new top-level folder — it is already tracked on the base (`git ls-files docker`: `Dockerfile.api`, `Dockerfile.web`, `nginx.conf`, `api.settings.example.json`, `sample-library/`), superseding the intake F1 absence note; adding `docker/ollama-init.sh` follows the Q6-approved envelope (Q6/12 above; `tasks.md` T006) and needs no further folder authorization.
- **ADR numbering (Plan-challenge L1; `tasks.md` T003)**: `0024` is taken by DEV-413 (`origin/feature/DEV-413`: `docs/adr/0024-movie-features-storage-port.md`, `specs/DEV-413/adr-0024-movie-features-storage-port.md`) and the highest on `origin/main` is `0023` (`recon-DEV-349` DEV-349 note:38), so Anvil must use the next sequential `ADR-0025`, not `0024`, preserving the L1 rename/Accepted lifecycle.

---

## CI e2e isolation — bounded second remediation round (2026-10-22)

[from Patron] APPROVED: authorize narrow envelope expansion for `web/e2e/run.mjs` only, as Keel recommends; Keel persists this in `brief.md` before Cog handoff and no code write is authorized until that record exists.

- **Basis (§2.3 item 6, envelope amendment)**: profile-less e2e `compose up` at `web/e2e/run.mjs:523-525` selects the profile-less Ollama pair at `docker-compose.yml:15,28`, violating authoritative default-CI isolation (`brief.md` Authoritative delivery: default CI does not start Ollama; `recon-DEV-349:60-66,76`). The confirmed Medium Spec/Risk finding is in scope; the exact final Compose exit cause remains unknown (`recon-DEV-349:73,84`), so no registry, timeout, or pull-failure diagnosis is adopted.
- **Approved change**: in `web/e2e/run.mjs` only, add explicit existing service targets `postgres rabbitmq api web omdb-stub` to the stack `up`, preserving `-d --wait --wait-timeout` and all budgets. No Compose profile change, no `docker-compose.yml` edit, no dependency/budget change, and no change to ticket-required plain `docker compose up` model-ready developer behaviour (Q4/12). `README.md` may clarify only that browser smoke is isolated, only if needed.
- **Scope and proof**: this is one bounded second remediation round within the Q8 round cap; original Low follow-ups remain untouched and no other file is authorized. Cog must show the e2e `up` no longer selects `ollama`/`ollama-model-init` and that the listed stack starts; failure of the unknown original exit tail is not claimed fixed beyond removing the confirmed Ollama selection.

