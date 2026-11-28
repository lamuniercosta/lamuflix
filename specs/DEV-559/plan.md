# Implementation Plan: README overhaul and operations docs move

**Branch**: `feature/559-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-559-spec`) | **Date**: 2026-11-28 | **Spec**: `specs/DEV-559/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q10, Q9 Keel amendment); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-559`, `recon-DEV-559`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft (Spec Kit Phase 2 step 3.2). Gate 1 and Build remain pending. Quill does not claim freeze.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md` (specify quality only), `DRAFTING_RECEIPT.md`. No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — grill freezes inventable unknowns; no schema or API change. Conductor ask omitted `/speckit-clarify` and `/speckit-checklist`.

## Summary

Overhaul `README.md` so the top is short, current, and honest; move operations H2s into nine `docs/operations/*.md` files; relocate the modernization story and inline C4 to `docs/architecture/modernization.md` and currency-fix `c4-container.md`; credit OMDb/TMDb; replace the library screenshot; re-point `SubtitleSetupDocumentationTests` and vocabulary exceptions; track root `PRODUCT.md` after scrub. Docs-only: nothing under `src/` or `web/src/`.

## Technical Context

**Language/Version**: Markdown documentation; one C# unit-test file under .NET 10 (`tests/LamuFlix.UnitTests`). No production C# or TypeScript changes.

**Primary Dependencies**: Existing repo docs layout (`README.md`, `docs/architecture/`, `docs/images/`, `docs/adr/`), `vocabulary-check.json` + `npm run check:vocabulary`, ADR 0023 import path for screenshot seeding, CHANGELOG `v0.1.0`. No new NuGet/npm dependency or tooling (`brief.md` Frozen scope; `PRODUCT.md` §5 item 1).

**Storage**: N/A — no database or migration change. Migration *wording* in docs becomes count-free (`recon-DEV-559`: 21 migration files).

**Testing**: Existing `SubtitleSetupDocumentationTests` (path + README-name renames only, Q9 amended). Manual/scripted link resolution and heading inventory (no new tooling). On-the-day command table. Vocabulary check. Three static gates + format on the single changed `.cs`. Mutation N/A (no production code).

**Target Platform**: Repository documentation consumed by the home-cluster owner; local Compose quick start.

**Project Type**: Documentation overhaul with one unit-test path retarget inside an existing .NET / React personal streaming platform.

**Performance Goals**: N/A (docs). Screenshot and any required logo each under 1 MB.

**Constraints**: Charter §2.3 items ruled in CONCLUSIONS (no owner checkbox). No LocalPlay execution, secrets, or Compose `-v` teardown during verification (Q6). Round cap 1 review + 1 remediation. Size M requires Risk (Sentry), Standards (Ledger), Spec (Compass). PRODUCT.md scrub stop-to-Bernstein on private data.

**Scale/Scope**: Size M; README + nine operations files + modernization.md + c4/README touch + one image (+ optional logo) + one test file + vocabulary re-points + tracked PRODUCT.md. Parent DEV-100.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No structural owner checkbox (`CONCLUSIONS.md` §2.3).

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; ArchitectureTests | **N/A** — no production/Core change; ArchitectureTests unchanged. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no use case. |
| III–V | MovieQuery / mutators / ProblemDetails | **N/A** — no domain or API change. |
| VI (Observability) | Safe diagnostics | **PASS** — observability doc moves with re-pointed image; no new telemetry; no secrets in docs. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — Q6 marks LocalPlay and secret-bearing commands not run; no Process.Start; Compose teardown without `-v`. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — vocabulary exceptions re-pointed only (Q10); no CONTEXT.md expansion; no new banned phrases. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — existing unit test retargeted; no new test framework; assertions unchanged (Q9 amended). |

### Stack / workflow checks

| Check | Plan response |
|---|---|
| Documentation Rules (constitution: README MUST carry modernisation narrative, C4, trace screenshot, compose, gates, ADR links) | **PASS under ticket authority** — Wanted 2/4 and brief Q1–Q2 place narrative/C4/operations under `docs/` with README links and quick start retained; Patron recorded no constitution-departure checkbox (`CONCLUSIONS.md`). Trace screenshot remains reachable via `docs/operations/observability.md`. |
| Static-analysis gates | Single changed `.cs` must pass Roslyn, complexity ≤15, InspectCode; `dotnet format --verify-no-changes`. |
| Web gates | Only `npm run check:vocabulary` in `web/` (`.github/workflows/vocabulary.yml`); other web gates N/A (`web/src` unchanged). |
| Mutation | **N/A** — no production code under `src/`; report honestly, never as PASS. |
| OpenAPI / contract chain | Untouched. |

### Complexity Tracking

No constitution violations requiring justification. No new architectural layer or dependency.

## Approach

- **Phase A** — Quill drafts Spec Kit artifacts; Keel analyzes; Bernstein/Rigger Spec Kit PR; user Gate 1 merge; Build after receipt and pickup.
- **Phase B pickup** (`brief.md` Task ordering step 1): re-run `/speckit-analyze` against `main`; record DEV-572 merge state for Q7 wording; confirm `vocabulary-check.json` path and entries at lines 16, 21, 63, 68, 132; take "before" heading inventory from README at pickup base. Read-only; stop `needs recon:` / cited Patron ruling on protected expansion.
- **Task order** (`brief.md` Task ordering):
  1. Pickup (above).
  2. Create `docs/operations/*` from README, re-point links/images, update `vocabulary-check.json` in the **same commit** as the text move.
  3. One commit: point `SubtitleSetupDocumentationTests` at `subtitles.md` (path + README renames only) and remove Subtitles from README; test stays green every commit.
  4. Move story/C4 to `modernization.md`; update both diagrams and `c4-container.md:15`.
  5. Currency pass + on-the-day command runs; fix docs.
  6. Rewrite README top (lists, quick start, credits with terms checked on the day, operations links).
  7. Capture screenshot; replace `library.png`.
  8. Track `PRODUCT.md` after scrub.
  9. After inventory, link check, vocabulary, gates/tests, PR-body evidence (incl. DEV-558 verified-vacuous).
- **Do not**: edit `src/` or `web/src/`; add tooling/deps; use main-checkout untracked `docs/operations/`; run LocalPlay/secrets/Compose `-v`/mutation; invent marketing copy; merge PRODUCT.md into `specs/PRODUCT.md`; touch main checkout; claim the missing `v0.1.0` tag.

## Verification Approach

- Heading inventory before/after in PR body (FR-005 / SC-002).
- Command table: file, command, ran or not run, result or reason (Q6 / FR-011).
- Terms URLs, check dates, verbatim credit wording in PR body (FR-010).
- Screenshot size under 1 MB + visual library-grid check (FR-009).
- Corrected README :5, migration claims, `c4-container.md:15` (FR-007).
- `PRODUCT.md` tracked commit after scrub (FR-012).
- DEV-558 verified-vacuous recorded (FR-015 / Q4).
- `dotnet test` incl. `SubtitleSetupDocumentationTests` (FR-013).
- Roslyn, complexity ≤15, InspectCode on changed `.cs`; `dotnet format --verify-no-changes`.
- `npm run check:vocabulary` in `web/`.
- Every relative link/image in README, `docs/operations/*`, `modernization.md`, `docs/architecture/README.md` resolves (hand or existing script; no new tooling).
- Three axis reports; round cap 1+1; no unresolved Critical/High.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-559/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (mirrored to private IMPL_PLAN)
├── tasks.md             # Mirrored to private TASKS
├── DRAFTING_RECEIPT.md
└── checklists/
    └── requirements.md  # specify quality checklist only
```

Private Spec Kit mirrors: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-559\plan.md` and `tasks.md` (byte-identical to canonical `specs/DEV-559/` copies).

### Source Code (authorized envelope — brief Files touched)

```text
README.md
PRODUCT.md                                          # newly tracked (copy from main checkout path)
vocabulary-check.json                               # file: re-points only
docs/operations/
├── docker-compose.md                               # new
├── windows-daily-driver.md                         # new
├── home-deployment.md                              # new
├── subtitles.md                                    # new
├── ollama-embeddings.md                            # new
├── library-relative-upgrade.md                     # new
├── verification.md                                 # new
├── observability.md                                # new
└── local-dotnet-tools.md                           # new
docs/architecture/
├── modernization.md                                # new
├── c4-container.md                                 # update
└── README.md                                       # only if architecture-decisions prose
docs/images/
├── library.png                                     # replace in place
└── (optional official provider logo under Q5)
tests/LamuFlix.UnitTests/SubtitleSetupDocumentationTests.cs
```

**Structure Decision**: Existing docs tree plus new `docs/operations/` subfolder (ticket file map; §2.3 item 2 not triggered). No `src/` or `web/src/` edits.

## Phase 0 / Phase 1 Spec Kit outputs

- **research.md**: omitted — grill Q1–Q10 (incl. Q9 amendment) freeze inventable unknowns.
- **data-model.md / contracts/**: omitted — no schema or public API change.
- **quickstart.md**: omitted — verification is the brief Done-when evidence list and Q6 command table.
