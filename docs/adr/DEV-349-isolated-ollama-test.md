---
status: proposed
---

# Isolate the DEV-349 real-model Ollama test

DEV-349 explicitly requires one real-model Testcontainers check through the DEV-412 adapter, while default tests use the deterministic fake. The constitution explicitly permits the isolated Ollama category outside default CI (.specify/memory/constitution.md:526-527, quoted in recon-DEV-349:70). Patron Q2 reconciles ADR-0012:15 as the default-suite policy: the ticket-specific opt-in exception does not introduce hosted embedding APIs or change the default policy.

Keep the real-model check isolated and explicitly opted in. Default tests and CI must never start Ollama or download its model. DEV-412 owns adapter contract tests; DEV-349 owns the real-model check and Compose provisioning. Phase B requires the DEV-412 runtime adapter merged into main and present on the build base; Ready to build alone does not meet this prerequisite (recon-DEV-349-2:54-60; Patron Q1).

This ADR is proposed until Gate 1. It does not claim implementation, runtime proof, plan freeze, or Gate 1 approval. Full cited decision exchanges: specs/DEV-349/CONCLUSIONS.md Q1-Q2.
