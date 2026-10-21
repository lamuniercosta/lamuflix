# Implementation Plan: DEV-349 Ollama provisioning and isolated real-model proof

**Branch**: `feature/349-spec` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), settled grill brief [brief.md](brief.md), facts from notes `recon-DEV-349` and `recon-DEV-349-2`, Patron rulings in [CONCLUSIONS.md](CONCLUSIONS.md), taste receipt [ASSUMPTIONS.md](ASSUMPTIONS.md), `specs/PRODUCT.md`, proposed ADR [docs/adr/DEV-349-isolated-ollama-test.md](../../docs/adr/DEV-349-isolated-ollama-test.md) (unchanged and proposed through Phase A and Gate 1; Phase B rename only after authorized T001/T003 lifecycle).

**Status**: Draft planning artifact only (Phase A). Not frozen; Gate 1 not claimed. No gates or implementation are started by this ask. Build waits until the separate spec PR merges (Gate 1) and Phase B entry prerequisites are met.

## Summary

Add an Ollama Compose service with a one-shot model initializer, persistent model volume, and model-aware health so compose up finishes ready only when the configured `all-minilm` model is present. Add one isolated opt-in Testcontainers integration test that exercises the merged DEV-412 `IEmbeddingGenerator<string, Embedding<float>>` registration/startup seam against a fresh Ollama container and asserts exactly one 384-dimensional embedding for a fixed sample plot. Keep default unfiltered tests and CI free of Ollama startup via `Category=OllamaIntegration` plus process opt-in and an explicit CI category exclusion. Document provisioning and the opt-in command in README and `.env.example`.

**Note on the Spec Kit phases**: research, the data model, contracts, and the quickstart are folded into this plan. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` is produced. Provisioning shape, isolation contract, proof boundary, file envelope, budgets, and gate expectations are already ruled in brief Q1–Q8; this ticket adds no HTTP route and no persistence entity. `.specify/extensions.yml` is absent, so no extension hook ran. There is no agent-context script under `.specify/`; `AGENTS.md` is outside the Phase A artifact set and is not edited. The proposed ADR remains unchanged and proposed through Phase A and Gate 1; Quill does not edit or number it now.

## Technical Context

**Language/Version**: C# on .NET 10 (solution projects under `src/` / `tests/`); Compose YAML and a POSIX init script for the one-shot puller.

**Primary Dependencies**: None added in this ticket. Reuse existing Testcontainers primitives already available through current test dependencies (`Testcontainers.PostgreSql` / `Testcontainers.RabbitMq` pins at `Directory.Packages.props:14-15`, `recon-DEV-349:32`). DEV-412 supplies OllamaSharp + Microsoft.Extensions.AI adapter packages and host registration on the Phase B build base (brief Q1; recon-DEV-349-2:31-35). No `Testcontainers.Ollama` module and no new NuGet pin in DEV-349 (brief Q5).

**Storage**: Persistent Compose volume `ollama-models` for developer model retention; fresh isolated test storage that must not reuse that volume. No database schema or pgvector table work (brief Authoritative delivery; recon-DEV-349:68).

**Testing**: xUnit v3, Shouldly, existing IntegrationTests project. One trait-tagged opt-in test; default fake embedding tests remain untouched (`recon-DEV-349:55-59`). No Gherkin/Reqnroll. No new property invariant; existing property suite runs normally (brief Gate expectations).

**Target Platform**: Local Docker Compose development host and CI GitHub Actions Ubuntu runners that must never start Ollama by default (`recon-DEV-349:30`; brief Q3).

**Project Type**: Existing .NET solution using ports and adapters. No new project.

**Performance Goals**: None in the brief. None added. Operational deadlines are configurable defaults (120s / 900s / 120s / 2s + finite cleanup), not harness thresholds (brief Q6).

**Constraints**: No new package/project/layer; no API depends_on Ollama; no required public Ollama port; no hosted embedding API; no nomic/truncation/padding/dimensions override; no `EmbeddingDimensions.Validate` change; no shared fixture rewrite; thresholds stay in `harness.yml`. Phase B entry blocked until PRODUCT Epic 7 completion evidence is present (`specs/PRODUCT.md:17`) and DEV-412 runtime is merged to main and present on the build base (brief Q1); Compose plus isolated proof is not a sequencing exception.

**Scale/Scope**: Patron-approved Q6 file envelope only. Delivery scope (brief Frozen scope): configurable all-minilm Ollama Compose service/initializer with model-aware health; one isolated opt-in real-model 384-dim check through merged DEV-412; default-suite/CI isolation. Scope statements describe that boundary and do not preclude findings about failures to meet in-scope requirements.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Principle I (boundaries): no new project, folder, or architectural layer. Test stays in existing `LamuFlix.IntegrationTests` (brief Q5; recon-DEV-349:27,66-67).
- Principle number: N/A (supporting constitution section) — Technology Stack embeddings row (`constitution.md:346`); nearest applicable Principle I / IX: Ollama via OllamaSharp at runtime; deterministic fake in default tests — DEV-412 owns the adapter; DEV-349 owns Compose + real-model check (recon-DEV-349-2:31-35,60).
- Principle number: N/A (supporting constitution section) — Known trade-off (`constitution.md:526-527`); nearest applicable Principle IX: embedding model download slows first compose start; tests default to the fake; the single Ollama Testcontainers test sits in its own category — matches brief Q2–Q3.
- Principle number: N/A (supporting constitution section) — ADR-0012:15 default fake-only policy; nearest applicable Principle IX: preserved; isolated opt-in exception recorded in proposed `docs/adr/DEV-349-isolated-ollama-test.md` without rewriting ADR-0012 (brief Q2).
- Principle VI (health checks): Compose health and startup probes are not registered ServiceDefaults `/health/ready` checks. Host health-check compliance is owned by DEV-412, the runtime adapter/host-wiring slice; DEV-349 owns container Compose health and test-local proof, with no `src/` host wiring. This is ownership, not an assertion that a health check exists or that unverified DEV-412 spec details are known. Existing T001 verification of the merged predecessor registration/startup seam remains binding (brief Plan-challenge amendment; CONCLUSIONS Plan-challenge L1/L2; constitution.md:253-258; recon-DEV-349-2:31-35,58-60,64-65).
- Principle VII (configuration/secrets): `.env.example` documents non-secret Ollama image/model/operational settings only; no secret literals (brief Q6; recon-DEV-349:70).
- Principle IX (tests): real-model proof uses Testcontainers; default suite stays fake; opted-in infra failure fails closed (brief Q3, Q5).
- Principle number: N/A (supporting constitution section) — API and Contract Rules; nearest applicable Principle I: no public route/DTO/OpenAPI/TS change (brief Authoritative delivery; recon-DEV-349:69).
- Principle number: N/A (supporting constitution section) — Documentation rules (`constitution.md:499-507`); nearest applicable Principle I: Quill does not author or number ADRs in Phase A; proposed ADR remains unchanged and proposed through Phase A and Gate 1; Phase B lifecycle (T001 number recon, Anvil rename/Accepted, T013 README link, T015 verify) is authorized by the Plan-challenge amendment (constitution.md:499-501,505; brief.md:89-97). README edit is in the approved envelope (brief Q6).
- Principle number: N/A (supporting constitution section) — Static-analysis gates (`constitution.md:366-389`); nearest applicable Principle IX: Roslyn, complexity, InspectCode, mutation, property, vulnerable packages, format, `dotnet test` — harness thresholds authoritative, never copied into this plan as numeric literals and never lowered (brief Gate expectations).
- Principle number: N/A (supporting constitution section) — Charter §2.3 / PRODUCT §5; nearest applicable Principle I: care-list items ruled in CONCLUSIONS Q1–Q8 and Plan-challenge L1/L2; zero owner escalations (brief Q8; brief.md:89-97).

Post-design re-check: the same checks hold. This plan adds no package, project, schema, HTTP contract, CONTEXT.md edit, or file outside the approved brief envelope (Phase B ADR lifecycle rename/Accepted remains inside the amended envelope). Side design files are not produced — the brief already freezes contracts, files, ordering, and proofs. Proposed ADR file content is not modified in Phase A.

## Scope and prerequisite checks

Binding product, care-list, and sequencing constraints. Constraints remain in force.

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The slice matches DEV-349:22 (compose Ollama + health + model pull; isolated Testcontainers 384-dim proof; default CI does not start Ollama). Nothing is added or dropped. No owner checkbox.
- Phase A may draft against the documented DEV-412 contract (all-minilm default, mandatory actual 384 output, typed HttpClient seam, fail-closed startup probe, real-model check deferred to DEV-349) (recon-DEV-349-2:11-35,58). Phase B entry requires merged DEV-412 runtime on the build base (brief Q1).
- No schema or migration (care list item 3; brief; recon-DEV-349:68).
- Public API shape unchanged (care list item 4; recon-DEV-349:69).
- Epic / sequencing: PRODUCT defers Recommendations / Ollama / pgvector execution until Epic 7 (SPA) completes (`specs/PRODUCT.md:17`; brief.md:7; Q1–Q8 grant no exception). Phase B entry includes a bounded recon check that Epic 7 completion evidence is present; missing evidence stops for recon. This ticket's delivery envelope is Compose provisioning plus the isolated proof seam and does not start recommendations ingest/query work; that scope boundary is not a sequencing exception.
- File-scope AC-forced CI edit and named support files are Q3/Q6 Patron rulings under Charter 2.3 item 6 (CONCLUSIONS Q3, Q6).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-349/
├── brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
├── spec.md                                     (/speckit-specify + clarify)
├── plan.md                                     (/speckit-plan)
├── tasks.md                                    (/speckit-tasks)
├── checklists/requirements.md                  (/speckit-checklist)
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. `AGENTS.md` is not updated. Proposed ADR path through Phase A and Gate 1 (unchanged content): `docs/adr/DEV-349-isolated-ollama-test.md`. After Gate 1, Phase B renames to T001-verified `docs/adr/ADR-00NN-isolated-ollama-test.md` (status Accepted) per brief Plan-challenge amendment.

### Source Code (repository root) — approved envelope only

```text
# Modify
docker-compose.yml
.github/workflows/ci.yml
.env.example
README.md

# Add
docker/ollama-init.sh
tests/LamuFlix.IntegrationTests/OllamaEmbeddingIntegrationTests.cs
tests/LamuFlix.IntegrationTests/OllamaFixture.cs
```

**Structure Decision**: Match existing root Compose + `docker/` support scripts and IntegrationTests layout (`recon-DEV-349:13-21,27`). No csproj/package/shared-fixture edits unless bounded Phase B recon surfaces an explicit blocker for Patron (brief Q6).

## Design (from settled brief)

1. **Predecessor (Q1)**: Plan and draft against DEV-412 contract facts in recon-DEV-349-2. Implement only after Wisp confirms merged adapter, registration/startup API, generic container API availability, image digest, CLI probe/pull, verified finite cleanup mechanics (ownership on all exits; separately enforceable deadline; failure diagnostics; no invented API or numeric budget), and next sequential `ADR-00NN` inventory on the Phase B base.
2. **Policy (Q2)**: Default suites remain fake-only (ADR-0012:15). Isolated opt-in exception is constitution- and ticket-authorized; proposed ADR records that and stays proposed/unchanged through Phase A and Gate 1; do not rewrite ADR-0012. Phase B ADR lifecycle (number, rename, Accepted) follows the Plan-challenge L1 amendment.
3. **Isolation (Q3)**: Trait `Category=OllamaIntegration`; env `LAMUFLIX_RUN_OLLAMA_TESTS=1` checked at the earliest fixture construction/startup path before any container, model, or resource object is created, or else all creation is fully lazy behind that gate (test-body gate may remain as defence in depth). CI Test step excludes the category. Gating stays in the new test/fixture files.
4. **Provisioning (Q4)**: `ollama` + `ollama-model-init` share pinned image and `ollama-models`. Health is model-aware. Initializer waits for server readiness with finite deadline, pulls the configured model as one fixed quoted CLI argument (reject unset/empty; no eval/code interpolation), exits 0 only on success; never waits on model-aware healthy before pull.
5. **Real-model proof (Q5)**: One IntegrationTests method; resolve real DEV-412 generator; assert one vector length `EmbeddingDimensions.Value`; ownership cleanup on all exits with a separately enforced finite cleanup deadline and failure diagnostics (mechanics from T001; no invented API or numeric budget); no substitute paths.
6. **Envelope/defaults (Q6)**: Only named files plus the Plan-challenge-authorized Phase B ADR lifecycle rename/Accepted transition; budgets and naming from ASSUMPTIONS; exact image pin and cleanup mechanics from Phase B recon.
7. **Ordering/proof (Q7)**: Phase B recon (including next sequential ADR-00NN) → Compose/init → test/CI/docs/ADR lifecycle → Gauge evidence set from the brief.
8. **Gates**: Brief Gate expectations; mutation scope-empty when no `src/` C#; property suite normal; web scope-empty if unchanged.

### Opt-in command (documentation and validation requirement)

```powershell
$env:LAMUFLIX_RUN_OLLAMA_TESTS = '1'
try {
    dotnet test tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj --filter "Category=OllamaIntegration"
} finally {
    Remove-Item Env:LAMUFLIX_RUN_OLLAMA_TESTS -ErrorAction SilentlyContinue
}
```

### Acceptance evidence (Gauge / Phase B)

1. Fresh-store Compose: initializer success + model-aware healthy.
2. Restart retention of the model.
3. Controlled readiness/pull failure not healthy.
4. Unfiltered default suite skips without Ollama startup/download.
5. CI filter excludes category; no opt-in in CI.
6. Explicit opt-in real-adapter test: one 384 vector + cleanup (including failure cleanup). Opted-in infra failure is failure. If the explicit run cannot execute, required runtime proof is missing.

## Complexity Tracking

> No constitution violations requiring justification. Compose provisioning + one isolated opt-in test reuse existing IntegrationTests and Testcontainers primitives; adapter ownership stays with DEV-412.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Phase boundaries

- **Phase A (this ask)**: Spec Kit docs under `specs/DEV-349` only (`spec.md`, `plan.md`, `tasks.md`, checklist, drafting receipt). Proposed ADR left unchanged and proposed through Gate 1. No freeze, publish, gates, or implementation.
- **Phase B**: After Gate 1, workspace handoff, Q1 predecessor satisfaction, and PRODUCT Epic 7 completion evidence at entry (`specs/PRODUCT.md:17`), Anvil implements per `tasks.md` one phase per ask; Cog refactor; Gauge gates and acceptance evidence. Fresh Phase B drift/prerequisite recon remains the first implementation task (including ADR-00NN inventory); missing Epic 7 or DEV-412 evidence stops for recon. ADR rename to `docs/adr/ADR-00NN-isolated-ollama-test.md` with status Accepted is authorized only after T001 records the number.
