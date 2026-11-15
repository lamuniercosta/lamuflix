---
status: proposed
---

# Keep shipped Compose startup coverage separate from stubbed smoke coverage

DEV-471 requires a per-PR Compose smoke and proof that reverting DEV-440 fails. The existing e2e overlay overrides the same Ollama endpoint supplied by production fix 4646476f, so overlay-only tests could pass after that production fix is reverted. One required check therefore first proves healthy startup of plain shipped Compose with real Ollama/model readiness, then exercises import, enrichment, thumbnail, library listing, hub updates and browser playback on a separate isolated shipped-base-plus-stub-overlay stack; it preserves LocalPlay disabled and the shipped nginx process-play block.

This accepts real model provisioning and a second stack lifecycle to retain both production configuration coverage and deterministic external-provider flow coverage. Delivery evidence must show the same plain-startup assertion passes on the candidate and fails for the expected API startup cause on a disposable controlled revert, without mutating the PR checkout; repeat that proof when the runner or assertion changes. Basis: Patron Q2–Q6 in specs/DEV-471/CONCLUSIONS.md and recon-DEV-471 DEV-440 commit/expected-regression evidence. This ADR records the grill decision; proposed status remains until Gate 1, and no live runtime proof is claimed.
