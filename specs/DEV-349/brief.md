# DEV-349 - Ollama provisioning and isolated real-model proof

## Status and authority

Phase 2 step 1 grill closed with Patron at Q8/12 on 2026-10-21. Shared understanding confirmed; zero owner structural checkboxes. Decision source: full verbatim exchanges in CONCLUSIONS.md Q1-Q8. Taste defaults: ASSUMPTIONS.md. This is the handoff to Quill for drafting, not plan freeze, Gate 1, implementation, runtime proof, or merge approval.

Worktree: F:/Dev/LamuFlix.worktrees/feature-349-spec. Branch: feature/349-spec. Recon base: 541dd85985b98f9f1664a3238e9ba384c6ba17bc. Task: DEV-349, parent DEV-288, size L, no ui tag (DEV-349:3-7,14-22). Facts are bounded to recon-DEV-349 and recon-DEV-349-2. PRODUCT.md and lamuflix-team-charter govern decisions. Missing facts go to the Conductor as needs recon; no expanded search or inferred runtime availability.

## Authoritative delivery and boundaries

DEV-349:22 requires an Ollama Docker Compose service with health check and model pull, plus an isolated Testcontainers test calling the DEV-412 adapter and asserting a 384-dimensional embedding for a sample plot. Compose up brings Ollama with the model when health passes; default CI does not start Ollama. DEV-325 supplies the existing compose file (recon-DEV-349:13). DEV-349 owns the real-model check; DEV-412 owns adapter implementation, host registration, startup validation, and WireMock contract/error-mapping tests (recon-DEV-349-2:31-35,60).

No schema, database, pgvector, metadata pipeline, API endpoint/DTO/status, production adapter, host wiring, new package/project/layer, shared Postgres/RabbitMQ fixture, local playback, secrets, or generated contract edits. No hosted embedding API. No nomic substitution, truncation, padding, or dimension override to manufacture 384 values. No API depends_on change or public Ollama port exposure is required.

## Every grill answer

1. **Q1 - Dependency:** Phase A closes against the documented DEV-412 contract. Phase B entry requires DEV-412 runtime adapter merged to main and present on the DEV-349 build base. Ready to build is a spec-handoff state, not implemented/merged runtime proof. No staged placeholder test or substitute adapter. Basis: recon-DEV-349-2:54-60; DEV-349:22.
2. **Q2 - Policy reconciliation:** The ticket and constitution 526-527 authorize the isolated opt-in real-model exception. ADR-0012:15 describes default fake-only tests and no hosted API. No constitution departure or ticket change is needed; no owner checkbox. Proposed ADR: docs/adr/DEV-349-isolated-ollama-test.md, proposed until Gate 1. Preserve ADR-0012. Basis: recon-DEV-349:46,70; PRODUCT sections 3-5.
3. **Q3 - Isolation:** Category=OllamaIntegration and process opt-in LAMUFLIX_RUN_OLLAMA_TESTS=1. Check opt-in before any Ollama fixture/container/model initialization. Ordinary unfiltered dotnet test skips this test without Ollama startup/download. Default CI also explicitly excludes the category and has no opt-in setting. After opt-in, Docker/model/readiness errors fail the test; they are not skips. Keep gating test-local. The CI edit is AC-forced. Basis: recon-DEV-349:24-33; DEV-349:22.
4. **Q4 - Provisioning:** Add ollama plus one-shot ollama-model-init sharing a pinned Ollama image and persistent ollama-models volume. Configurable model defaults to all-minilm per DEV-412. Health requires server availability and selected-model presence. Initializer starts on service_started and independently waits for server readiness with a finite deadline, then pulls and exits 0 only on success. It must not depend on model-aware service_healthy before pulling. Failure cannot yield model-ready healthy status. Development restart retains the model; the test has fresh isolated storage. Basis: recon-DEV-349:13-20; recon-DEV-349-2:13-21,33-35.
5. **Q5 - Real-model proof:** One real-model test in existing LamuFlix.IntegrationTests using existing generic Testcontainers primitives. No new module or package. Start the pinned image with fresh storage and a dynamically mapped endpoint; pull all-minilm. Resolve the actual merged DEV-412 IEmbeddingGenerator<string, Embedding<float>> via its real registration and validated startup seam using the test endpoint/model. Generate the fixed nonempty sample plot and assert exactly one embedding, length EmbeddingDimensions.Value (384). No golden vector, fake, direct raw-client substitute, or database. Dispose owned resources on success and failure. Actual API availability/registration names require Phase B confirmation. Basis: recon-DEV-349:25-33,50-58; recon-DEV-349-2:31-42,59-60.
6. **Q6 - Envelope/defaults:** Only the named files below. Same verified Ollama image digest across Compose, initializer, and test; exact pin is a Phase B recon fact. Configurable defaults: 120 seconds server readiness, 900 seconds model pull, 120 seconds generation, 2 seconds polling; cleanup has its own finite allowance. Test/sample naming is fixed below and in ASSUMPTIONS.md. No new domain glossary term or CONTEXT.md change. No project/package/shared-fixture edits without a new bounded blocker and Patron ruling. Basis: recon-DEV-349:13-20,25-33,64-70; charter 2.3.
7. **Q7 - Ordering/proof:** Bounded Phase B prerequisite/drift recon precedes all implementation; Compose/init precedes test/CI/docs; Gauge verifies after implementation. Acceptance evidence covers clean-store provisioning, model-aware health, restart retention, controlled failure, default isolation, actual opted-in adapter output and cleanup. Gates keep existing thresholds and script-owned verdicts. Existing property suite runs normally; no new property invariant/framework. Details below. Basis: DEV-349:22; recon-DEV-349:26-33,50-58; recon-DEV-349-2:54-60.
8. **Q8 - Closure:** Patron confirmed shared understanding, zero owner structural checkboxes, the loop terms below and authorization to record these decisions and the proposed ADR. Nothing in this brief closes Gate 1 or authorizes production implementation at this phase.

## Planned file set

Modify only these existing files during Phase B:

- docker-compose.yml - ticket-named service, initializer, persistent volume, model-aware health and settings.
- .github/workflows/ci.yml - AC-forced default exclusion of Category=OllamaIntegration; no opt-in variable.
- .env.example - non-secret Ollama image/model and operational configuration documentation only.
- README.md - provisioning, health/readiness, first download, settings, failure diagnosis, and exact isolated invocation.

Add only these implementation files:

- docker/ollama-init.sh - finite server wait, pull, explicit failure, completion status.
- tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs - isolated test and pre-initialization opt-in gate.
- tests/LamuFlix.IntegrationTests/OllamaFixture.cs - test-local isolated container/model/startup/cleanup support.

Phase A decision artifacts: this brief, CONCLUSIONS.md, ASSUMPTIONS.md and docs/adr/DEV-349-isolated-ollama-test.md. Quill drafts the later Spec Kit artifacts; Keel does not author spec.md, plan.md or tasks.md in this ask. Existing unrelated text must be preserved.

## Test contract and commands to document

Class/method: OllamaEmbeddingIntegrationTests.GenerateAsync_SamplePlot_Returns384DimensionalEmbedding.

Fixed sample plot: `A botanist follows a signal from an abandoned observatory and discovers a hidden garden.`

Default `dotnet test` must skip before constructing any Ollama resource. CI additionally uses `--filter "Category!=OllamaIntegration"`. The manual opt-in command targets the existing integration test project:

```powershell
$env:LAMUFLIX_RUN_OLLAMA_TESTS = '1'
try {
    dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter "Category=OllamaIntegration"
} finally {
    Remove-Item Env:LAMUFLIX_RUN_OLLAMA_TESTS -ErrorAction SilentlyContinue
}
```

These commands are future documentation and validation requirements; none ran in this phase. Timeouts must fail with actionable diagnostics and deterministic disposal. Test storage must not reuse the developer model volume. Expected output is actual native 384-dimensional model output, not merely configured metadata or a requested dimensions value. DEV-412 null-metadata/probe resolution remains DEV-412-owned; do not change EmbeddingDimensions.Validate or mask failure in this ticket (recon-DEV-349-2:21,25-27,64).

## Task ordering and acceptance evidence

1. **Prerequisite/drift recon:** At Phase B entry Wisp confirms the merged DEV-412 runtime adapter on the build base, actual registration/startup API, generic container API availability through existing references, selected image version/digest, available image CLI/probe/pull mechanics, configurable finite deadlines and cleanup mechanics. Also reconcile DEV-325 Compose and current CI/test files. Recorded spec handoff is insufficient. Missing evidence stops to recon; off-envelope changes stop to Patron. No invented registration/class names, pins, package additions, or compatibility claims.
2. **Compose and initialization:** Implement the named service, initializer, persistent model storage, configured all-minilm, model-aware health and finite initialization. Initializer readiness does not wait on model-aware health. Preserve existing services and configuration.
3. **Isolated proof, CI and docs:** Add test-local opt-in gating before resources, isolated container and fresh model store, actual DEV-412 registration/startup and one-vector 384 assertion, bounded operations and disposal. Add the default CI exclusion and scoped README/env documentation. Do not change shared fixtures or default fake tests.
4. **Gauge verification:** Collect fresh-store Compose evidence showing successful initializer and model-aware healthy service; show model retained across restart. Deliberate readiness/pull failure must not claim healthy completion. Collect unfiltered default-suite and CI-filter evidence showing no Ollama startup/download. Run the explicit opt-in real-adapter test and record actual one-vector 384 output and cleanup, including failure cleanup. Opted-in infrastructure failure is failure, not a skip. If the explicit run cannot execute, required runtime proof is missing and delivery cannot be declared complete.

## Gate expectations

No tests, gates, analyzers, format, build or commits run during this Phase A ask. Later Gauge uses the unchanged pipeline: applicable Roslyn, complexity, InspectCode, property, vulnerability, format, and dotnet test gates. Existing property tests run normally; no property opt-out is requested. No web changes are planned; the web gate reports its own scope-empty result. No production C# under src is planned; mutation reports SKIPPED (scope-empty) only when the script returns that verdict. No new property/acceptance framework or redundant adapter contract tests.

Exit 1 and Could not run block. Exit-2 scope-empty SKIPPED is nonblocking and never PASS; configured harness opt-out SKIP and mutation-only N/A retain their distinct meanings. No gate threshold may be lowered. A source change or broader scope requires a new decision and re-evaluation of gate applicability.

## Loop discipline

**Closing bar:** No confirmed in-scope Critical, High, or Medium findings; ticket AC and proof obligations met; all applicable configured gates satisfied with accurate script-owned nonblocking outcomes. Low findings receive charter-compliant dispositions; normally noted, no ticket. Broken behavior, user-requested follow-up and charter filing conditions still require Patron/Rigger routing. All three review axes are required on this L ticket during the later review phase.

**Frozen scope:** Configurable all-minilm Ollama Compose service and model initializer with model-aware health; one isolated opt-in real-model Testcontainers 384-dimensional check through the merged DEV-412 adapter; default-suite/CI isolation and the Q6 support-file envelope; anything else is a follow-up issue, not a finding in this round.

This scope sentence does not authorize automatic tracker issue creation; the charter governs recording and filing.

**Round cap:** Two drafting/analysis correction rounds, two plan-challenge correction rounds, and two formal review/remediation rounds, counted independently in their respective later phases. Unresolved above-bar findings at the cap stop and report the blocker. Do not silently start another round or reduce the closing bar.

## Closeout and next authorized phase

Q1-Q8 are settled and recorded before closeout. No domain vocabulary was added; infrastructure names are taste defaults in ASSUMPTIONS.md. The proposed ADR and existing canonical glossary are ready inputs for Spec Kit; drafting must preserve their terminology and decisions. Quill may proceed with specify, clarify/checklist for L, plan and tasks under the Conductor handoff. Keel analysis, plan challenge/freeze, Patron provisional Gate 1, spec PR and user-owned merge remain separate later steps.


## Plan-challenge decision amendment - Patron L1/L2, 2026-10-21

This later cited ruling amends the Q6 ADR file envelope and supplements the compliance plan. Full verbatim request and ruling: CONCLUSIONS.md, Plan-challenge L1/L2. Zero owner checkboxes; no new Phase A recon. No freeze or Gate 1.

1. **L1 lifecycle and ownership:** Keep docs/adr/DEV-349-isolated-ollama-test.md unchanged and proposed through Phase A and the Gate 1 spec PR. After Gate 1, Wisp T001 inventories docs/adr/ on the Phase B origin/main base and records the next sequential ADR-00NN in recon-DEV-349. Anvil then renames the ticket ADR to docs/adr/ADR-00NN-isolated-ollama-test.md, changes status to Accepted, removes only the Phase-A-only proposed-until-Gate-1 disclaimer, and preserves approved Q1-Q2 decision text without re-authoring. No number is chosen in Phase A. This explicitly expands T003/T015 to allow this lifecycle change; T013 updates README link; T015 verifies numbered Accepted file, old path removed by rename, and ADR-0012 untouched. Basis: constitution.md:499-501,505; Patron ruling cites task-pipeline:18,83. No ADR edit now.
2. **L2 Principle VI:** Compose health and startup probes are not registered ServiceDefaults /health/ready checks. Host health-check compliance is owned by DEV-412, the runtime adapter/host-wiring slice; DEV-349 owns container Compose health and test-local proof, with no src/ host wiring. This is ownership, not an assertion that a health check exists or that unverified DEV-412 spec details are known. Existing T001 verification of the merged predecessor registration/startup seam remains binding. Basis: constitution.md:253-258; DEV-349:22; recon-DEV-349:15-20,42,57,69; recon-DEV-349-2:31-35,58-60,64-65.
3. **Numbering:** Every plan Constitution Check item cites its applicable Roman numeral Principle I-IX, including explicit VI; unnumbered supporting sections explicitly state Principle number: N/A (supporting constitution section) alongside nearest applicable numbered principle. Basis: constitution.md:550-551.
4. **Correction scope:** Quill incorporates this amendment into spec/plan/tasks/checklist/receipt consistency, including FR-016 and lifecycle prohibitions. S1-S3, C1-C2 corrections remain those in adjudication-DEV-349-plan-round1. Correct plan hyperlink to ../../docs/adr/DEV-349-isolated-ollama-test.md while the Phase A path exists; future rename links follow T001 verification. Prior Keel ../../../ link verification was erroneous and is retracted. Do not rewrite historical correction receipts.
5. **Round discipline:** One consolidated plan-challenge correction list, independent maximum two correction rounds. None completed at this amendment. No tests/gates/analyzers/format/build/commits, ADR edit, freeze, or Gate 1 claimed.

