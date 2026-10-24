# Implementation Plan: DEV-356 Offline Evaluation Harness (Hit Rate@10 and MRR)

**Branch**: `feature/356-spec` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/DEV-356/spec.md`, settled grill brief `specs/DEV-356/brief.md` (Q1–Q10; grill closed by Patron at Q10 of 12), facts from note `recon-DEV-356`, Patron rulings in `specs/DEV-356/CONCLUSIONS.md`, product authority in `specs/PRODUCT.md` sections 1–3 and 5.

**Note**: Planning artifact only (Phase 2 Spec Kit drafting). This plan does **not** clear plan challenge/freeze or Gate 1—those remain separate later steps (brief Q10). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until the Q2 conjunctive Phase B STOP evidence is complete. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path. Gate 1 is not claimed.

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-356` | Yes |
| **Q2 conjunctive STOP**: Phase B drift recon verifies merged DEV-416 implementation AND exact invocation/input/output contract; then reconcile this plan against that evidence. Record concrete deterministic serializer options, UTC parsing, ordinal stable-ID ordering, seed/`TimeProvider` mapping, and an evidence-backed intentional evaluation invocation mode for the approved executable. Do not invent a replacement ranker, callable seam, or missing DEV-416 details (brief Q2; CONCLUSIONS.md Q2; recon-DEV-356:18–20,39; Sentry F2 clarification; Compass F5) | Wisp → `recon-DEV-356` refresh (or successor note) → Keel plan reconciliation | Yes — all conditions required |
| Full-ticket reconciliation: cite Wisp recon for fuller Scope & Technical Design reconciliation or an explicit no-fuller-text receipt; reconcile before build without contradiction when fuller text exists (recon-DEV-356:15; brief Q10; Sentry F7 clarification). No unsupported self-certification | Wisp recon → Keel / Conductor handoff in `DEV-356` | Yes, when fuller text exists or is required by intake; no-fuller-text receipt still required |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-356` | Yes |
| Keel drift check or amend against brief/spec/plan before code | Keel handoff in `DEV-356` | Yes |

## Summary

Deliver an offline evaluation harness in a dedicated `tests/LamuFlix.Evaluation` xUnit v3 executable project: validate deterministic fixtures, replay already-qualified playback observations in timestamp/stable-ID order with strict as-of history, recompute real DEV-416 final ranking per observation under fixed seed/`TimeProvider`, compute HR@10 and MRR truncated at 10 without favorable filtering, and publish a versioned deterministic JSON metrics summary via `scripts/run-evaluation.ps1` for DEV-417. Keep implementation simple inside the evaluation project—no new production port, ADR, or glossary rewrite. Concrete predecessor DTO mapping remains a Phase B reconciliation obligation under the Q2 STOP.

## Technical Context

**Language/Version**: C# / .NET 10. Evaluation project is net10.0. No web change.

**Primary Dependencies**: Existing CPM packages only as needed for the evaluation executable (xUnit v3 and matching test/assertion conventions). No new NuGet or npm dependency (Q3). Project references only as required after Phase B DEV-416 contract recon—not a blanket UnitTests reference copy.

**Storage**: No schema or migration. No live DB/export integration for evaluation (Q4). Fixtures are checked-in JSON under `tests/LamuFlix.Evaluation/Fixtures/`. Generated summaries default to untracked `artifacts/evaluation/`.

**Testing**: xUnit v3; Shouldly; NSubstitute only where already conventional and needed; FsCheck property tests in existing `tests/LamuFlix.UnitTests/Features/Recommendations/` location for `[0,1]` bounds and ordering/future-prefix invariants (Q8; recon-DEV-356:26). Real DEV-416 integration on a fixed nonempty fixture with independently worked expected metrics. Testcontainers if real persistence/translation is exercised; never mock driver behavior; no EF InMemory. All gate/test execution belongs to Phase B / Build workspace after the Q2 STOP clears.

**Target Platform**: Repository-local offline harness invoked by PowerShell. No public HTTP surface. No new external health endpoint.

**Project Type**: New dedicated test/evaluation project under `tests/` (Patron-approved charter §2.3 items 1, 2, and 6). No new production layer or Core feature-folder cross-reference.

**Performance Goals**: None in the brief. None added.

**Constraints**: TreatWarningsAsErrors. Ticket summary + Patron rulings authoritative for this draft; fuller ticket text must be reconciled before build (Q10). No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. No schema/migration. No new production port/ADR. Mutation never uses `--since`. Thresholds never lowered. Scope-empty/opt-out/N/A/could-not-run never PASS. PRODUCT Epic 9 sequencing not waived (PRODUCT.md §2; recon-DEV-356:33). Q2 Phase B STOP is conjunctive and blocking for implementation.

**Scale/Scope**: Authorized file set in brief Q9. Unanticipated files return to Keel/recon. Exact DEV-416 invocation mapping is Phase B evidence, not Phase A invention.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket authority (`specs/PRODUCT.md` §3): available ticket summary (DEV-356:18) plus Patron elaborations; full Technical Design absent at intake (recon-DEV-356:15)—reconcile any fuller text before build (Q10). Owner structural checkboxes: none (Q10).
- Dependency care list (§2.3 #1; PRODUCT §5 #1): no new NuGet; existing CPM only (Q3).
- Architecture (§2.3 #2; PRODUCT §5 #2): dedicated evaluation project deliberately approved (Q3); no new production layer/port (Q9–Q10).
- Database schema (§2.3 #3): none (Q6, Q9).
- Public API shape (§2.3 #4): none; file contract only for DEV-417 (Q6).
- LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded (Q9).
- File scope (§2.3 #6): planned edits deliberately authorized (Q9).
- Principle I: evaluation consumes real DEV-416 ranking; does not rewrite Recommendations production code or add a new Core port (Q9–Q10; recon-DEV-356:30–31).
- Principle VII: injected `TimeProvider` and seeded `Random`; no secrets in source (Q4).
- Principle VIII: evaluation-internal vocabulary in this plan/spec (`Evaluation fixture`, `Playback observation`, `As-of history`, `Final ranking (DEV-416)`, `Metrics summary`) introduces no new bounded-context domain term under Q10; no CONTEXT.md or glossary rewrite is authorized. A later contrary fact stops for recon/ruling rather than silently overriding the constitution or approved boundary (Compass F4).
- Principle IX: xUnit v3 / Shouldly / FsCheck conventions; Testcontainers if persistence/translation exercised; never mock drivers (Q8; recon-DEV-356:29).
- Static-analysis gates: configured scripts; no threshold waiver; scope-empty SKIPPED never PASS (Q9).
- PRODUCT Epic 9 sequencing note retained (PRODUCT.md §2; recon-DEV-356:33). Patron-ratified epic-level interpretation: not an express Epic 7 blocking prerequisite for DEV-356; no new Epic 7 preflight blocker or owner checkbox (Sentry F8 rejected).

Post-design re-check: the design below adds the ruled evaluation project, script, fixtures, pure metrics/replay, and DEV-416 integration under the Q2 STOP, with no public HTTP contract, no schema migration, no new production port/ADR, and no harness edit. The same gates hold. Plan challenge closure, plan freeze, and Gate 1 are not claimed here.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-356/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md
├── tasks.md
└── DRAFTING_RECEIPT.md
```

`research.md`, `data-model.md`, `quickstart.md`, `contracts/`, and `checklists/` are not produced. The brief already freezes approach, files, metric/replay/command contracts, and proof obligations. There is no new public HTTP contract. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path (brief Q9–Q10; Conductor drafting ask).

### Source Code (repository root)

```text
# New evaluation project (authorized)
tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj
tests/LamuFlix.Evaluation/Fixtures/baseline.json
tests/LamuFlix.Evaluation/...   # validation, replay, metrics, output, tests (exact type names after Phase B)

# Entry command
scripts/run-evaluation.ps1

# Solution registration
LamuFlix.sln

# Property invariants (existing location)
tests/LamuFlix.UnitTests/Features/Recommendations/*PropertyTests.cs
tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj  # only if required evaluation reference

# Optional ignore for generated output
.gitignore  # narrow artifacts/evaluation entry only if needed
```

No `/web` edit. No `harness.yml` or gate-script edit. No migration file. No CONTEXT.md rewrite. No production ranking rewrite under `src/LamuFlix.Core/Features/Recommendations/` beyond consuming the real DEV-416 API surfaced after Phase B recon.

**Structure Decision**: Keep all harness logic inside the dedicated evaluation project invoked by the explicit script. Integrate real DEV-416 only after Q2 STOP evidence; do not invent a local ranker or fake seam. Register the project in the existing solution. Place FsCheck invariants in the existing UnitTests Recommendations property-test location (Q8).

## Design Overview

1. **Q2 Phase B STOP and reconciliation (mandatory first).** Before any code phase: verify merged DEV-416 implementation and exact invocation/input/output contract by drift recon; reconcile fixture catalog/ranking inputs and call sites against that contract; record concrete deterministic serializer options, UTC parsing, ordinal stable-ID ordering, seed/`TimeProvider` mapping, and an evidence-backed intentional evaluation invocation mode; cite Wisp recon for fuller-ticket reconciliation or no-fuller-text receipt. Stop rather than invent.

2. **Evaluation project and solution registration (Q3).** After all Phase B entry checks, scaffold `tests/LamuFlix.Evaluation` net10.0 xUnit v3 executable with minimal existing CPM packages and only necessary project references before or alongside first compilable test authoring. Register in `LamuFlix.sln`. Behavior still follows tests.

3. **Fixture validation (Q7).** Load JSON fixtures declaring `fixtureId`, immutable catalog/ranking inputs, and event rows (unique stable event ID, user/movie identities, UTC timestamp). Reject duplicates, malformed/nonfinite values, unresolved references; preserve all valid rows; accept empty event arrays.

4. **Strict as-of replay (Q4).** Order by timestamp then stable event ID (ordinal stable-ID ordering per TB002). Build history from timestamp < target only (all applicable users). Equal-time isolation. Score before exposing target. Fixed seed/`TimeProvider`. Recompute real DEV-416 final ranking per observation using the TB002-reconciled invocation—no stub, temporary compile seam, or fake ranking path. Fail on duplicate ranking IDs (Q5).

5. **Pure metrics (Q5).** HR@10 = hitCount/N; MRR truncated at 10 with zero for absences; all rows count; N=0 yields zeros.

6. **Summary serialization and command (Q6–Q7).** Emit exact ordered JSON fields using TB002-recorded serializer options; unrounded finite invariant-culture numbers; no volatile metadata. Verify ignore coverage and add the narrowly authorized `.gitignore` entry only if needed before first generated output. `scripts/run-evaluation.ps1` implements the recorded intentional evaluation invocation mode and drives validation → replay → publication; defaults repo-root-relative; exit codes as ruled, including ordinary write-failure integrity and native child nonzero exit plus terminating/nonterminating script-error propagation without truncated success. Incidental test execution is not the publication path. No prescribed write mechanism, concurrency/crash-recovery scope, or script syntax beyond the ruled contracts.

7. **Proof order (Q8; Compass F1/F2).** After STOP: project scaffold → fixture/metrics → as-of/leakage with in-memory metrics/model equality under full-fixture permutation (no premature publication) → real-ranker baseline with independently worked expected metrics (integration supplies the baseline; no published-byte requirement yet) → command/publication after serializer/publication exist, including (a) repeated successful identical-fixture command runs with published-byte comparison and (b) explicit published-byte comparison for equivalent original/permuted fixtures on that same nonempty real-ranker baseline → property invariants → applicable gates. Closing bar and two-round remediation cap per Q1/Q9. No earlier proof step depends on a later implementation.

## Loop discipline

- **Closing bar:** Critical, High, and Medium in-scope findings block; Low is recorded and nonblocking (Q1).
- **Round cap:** two review rounds maximum; at most two remediation fix commits per round; unresolved above-bar findings after round two go to user review on the PR without claiming closure (Q9).
- **Frozen scope:** offline replay through real DEV-416; HR@10/truncated MRR; validated deterministic fixture/command/JSON contract; Q8 proofs. Anything else is a follow-up issue, not a finding in this round (Q9).
- **Gate verdicts:** preserve scope-empty SKIPPED, opt-out SKIP, mutation N/A, and Could not run distinctions; never PASS (Q9).
- **Plan challenge / Gate 1:** not cleared by this document; remain separate later claims (brief Q10).

## Complexity Tracking

> No constitution violations requiring justification beyond deliberate Patron approvals already recorded. New evaluation project and necessary tracked edits are Patron-ruled under charter §2.3 items 1, 2, and 6 (Q3, Q9) without owner checkboxes. No ADR.
