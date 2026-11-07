# Implementation Plan: Windows daily-driver setup (host API + durable container services)

**Branch**: `feature/457-spec` | **Date**: 2026-11-07 | **Spec**: `specs/DEV-457/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-457`, `recon-DEV-457`, `recon-DEV-457-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution). Grill commit: `94657cd8`.

**Status**: PLAN FROZEN by Keel after independent Phase 2 step 6 read-back (2026-11-07); repairs R1–R9 verified. Next: Patron gate1: provisional then Rigger spec PR then user Gate 1. Owner Gate 1 closed; no implementation clearance.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief; no public API shape change. `ASSUMPTIONS.md` already exists from grill; Quill does not rewrite it.

## Summary

Operationalise the constitutional Windows daily-driver host: add a dedicated root Compose entrypoint for durable loopback Postgres and RabbitMQ, document host Ollama and host API/Vite startup plus a placeholder Playback example in `README.md` only, correct the README Ollama-boot claim without runtime changes, and — after DEV-458/DEV-459 delivery — record scrubbed real BS Player imported-movie acceptance.

## Technical Context

**Language/Version**: Documentation and Compose YAML only for implementation files. Host API remains existing C# / .NET 10 `src/LamuFlix.Api`; host frontend remains existing Vite/React `web/`. No new C# or TypeScript production code in frozen scope (Q6).

**Primary Dependencies**: Existing pins only. **No new packages** (Q6; PRODUCT §5 item 1). Official Windows Ollama installer (`OllamaSetup.exe`) is an operator host prerequisite, not a repo dependency (Q3, Q6; recon-DEV-457-2 §12).

**Storage**: Durable named volumes for daily-driver Postgres and RabbitMQ only (Q1–Q2). No database schema change.

**Testing**: No invented unit/property tests for prose/config; no tests mirroring YAML (Q7). Gauge validates Compose rendering via `docker compose -f docker-compose.daily-driver.yml config` with process-local non-secret validation values, explicit `127.0.0.1` bindings, named volumes, project isolation, host API readiness, retained imported-movie state via isolated scratch-library import (explicit `Import-Library.ps1` overrides), and retained RabbitMQ broker state via isolated durable test queue + persistent test message (not mounts alone; no application-queue consumption). Manual Windows/Ollama/import/player proof is separate evidence. Conductor records `propertyTests: opt-out — no new domain invariants in docs/config changes` in the task note (DEV-457 receipt 15 currently; reconfirm at T001) (Q7). Mutation runs only if eligible production C# changes appear (outside frozen scope; would need a fresh decision).

**Target Platform**: Windows daily-driver host — API and Vite on the host; Postgres/RabbitMQ in the dedicated Compose project; host Ollama at `http://localhost:11434`.

**Project Type**: Docs + infrastructure Compose entrypoint; no feature slice / API / web code.

**Performance Goals**: N/A beyond ordinary local personal-library startup.

**Constraints**: Q6 file boundary only; no secrets committed; LocalPlay remains gated; Compose LocalPlay off; no AppHost/full-Compose edits; no launcher/Process.Start code changes; never lower harness thresholds; honest gate labels; one entrypoint at a time.

**Scale/Scope**: Size M; personal library; operator documentation and verification.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Brief Q8: no ticket contradiction, no constitution departure, no owner structural checkbox, no new ADR.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers | **PASS** — no C# ports/layers; new Compose entrypoint only (Q1, Q6; charter §2.3 item 2 ruled). |
| II / V (Handlers / public API) | Stable public API | **PASS** — no route/DTO/status changes; existing play endpoint used for manual proof (Q4–Q6). |
| VII (Configuration / LocalPlay / Process.Start) | Gated LocalPlay; local secrets | **PASS** — placeholders only; user-secrets / ignored local settings; Compose LocalPlay off; manual exercise via existing gated endpoint; no launcher rewrite (Q2, Q4–Q5; constitution 444, 446–449, 538). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new glossary concept required (Q8); operational terms documented in spec without inventing domain vocabulary. |
| IX (Test Pyramid) | Honest evidence | **PASS** — Gauge Compose/durability checks + separate manual runtime receipt; no YAML-mirroring unit tests; propertyTests opt-out via Conductor task note (Q7). |
| Windows daily-driver host | Constitution 709–710 | **PASS** — dedicated Compose for container Postgres/RabbitMQ; host API/Ollama/Vite (Q1). |
| Governance / Static-Analysis Gates | Honest exits; never lower thresholds | Phase B / Build: follow task-pipeline Phase 3 step 5 — preserve configured commands and actual applicability; no blanket skip from absence of C#; report PASS/failure/SKIPPED/SKIP/N/A/Could not run/Opt-out honestly; Size M three-axis review under Q8 bar; two-round cap. |

### Complexity Tracking

No unjustified constitution violation. Complexity Tracking empty. Charter items 1/3/4 authorise none; items 2/5/6 ruled by Q1–Q4.

## Approach

- **Phase A (this draft)**: Quill writes spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts only. Keel runs read-only `/speckit-analyze` and plan challenge. Gate handoff after repairs: plan freeze → Patron gate1: provisional → Rigger spec PR → user Gate 1 merge → Keel append-only T000 receipt. Transient status narration lives in `DRAFTING_RECEIPT.md`, not here. No Build code/gates here.
- **Phase B pickup (after Gate 1)**: Confirm owner Gate 1 merge receipt; confirm delivered DEV-458 and DEV-459 receipts (plans alone do not prove delivery); Wisp exact-head drift recon for authorised implementation files and service/image/data-mount/config/startup contracts, including whether `docker-compose.daily-driver.yml` has Dependabot coverage (no coverage fact asserted in Phase A; proven gap → Patron tracker disposition, no `dependabot.yml` edit); Keel reanalysis on drift; Gauge baseline; reconfirm propertyTests opt-out receipt. Stop on unresolved facts/decisions — no invention (Q7).
- **Hard ordering (brief Q7 / step 6)**: Owner Gate 1 merge + delivered DEV-458/DEV-459 + T001 pickup MUST precede T002 onward. No implementation before that triad.
- **Dependency-ordered implementation (brief task order)**:
  1. Add `docker-compose.daily-driver.yml` (durable Postgres/RabbitMQ; named volumes; top-level dedicated non-default project `name`; explicit `127.0.0.1` port bindings; fail-closed credential references via operator environment or untracked Compose channel — no empty/working committed defaults; no explanatory YAML comments; reuse current image pins pending recon confirmation). Exact identifiers remain deferred.
  2. Update `README.md` only: Windows daily-driver section (ASSUMPTIONS title/order), host Ollama official install path with distinct listener/model/startup diagnostics, corrected startup-validator docs, host API/Vite commands, placeholder Playback example with loopback confirmation **before** LocalPlay enablement (delivered DEV-458 contract), stop/collision/troubleshoot guidance including same-image durability vs unsupported image-upgrade caveat.
  3. Gauge validates `docker compose -f docker-compose.daily-driver.yml config` with process-local non-secret values; rendered `127.0.0.1` mappings; mounts; project isolation/collisions/project-scoped destructive reset; host API connectivity/readiness only after host Ollama/`all-minilm` and production validator-enabled API boot evidence; retained imported movie via isolated scratch library (explicit Import-Library overrides) + retained broker state via isolated durable test queue/message through restart/recreate with volumes retained.
  4. Execute documented Windows/Ollama/import/player steps; write scrubbed `specs/DEV-457/runtime-acceptance.md` with delivery SHA = exact DEV-457 implementation HEAD exercised (or `Could not run` with reason); pre-review scrub for real paths/secrets/connection strings.
  5. Run configured harness checks per task-pipeline Phase 3 step 5 (no blanket C#-absence skip); Size M three-axis review under Q8 bar; remediate within two-round cap; user merges.
- **Tests**: No prose/YAML unit or property tests. Manual and Gauge evidence labelled separately from automated gates.
- **Refactor / architect / ship**: No production C# expected; mutation only if eligible C# appears (outside frozen scope needs decision). Web gates not claimed PASS for unchanged web code.

## Project Structure

```text
specs/DEV-457/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  runtime-acceptance.md                 (Phase B only; never prefilled PASS)

docker-compose.daily-driver.yml         (NEW — Postgres + RabbitMQ only)
README.md                               (EDIT — Windows daily-driver section + Ollama correction + Playback example)

# Explicitly untouched
docker-compose.yml
.env.example
docker/api.settings.example.json
src/LamuFlix.AppHost/**
src/**/*.cs                             (no production C# in frozen scope)
web/**                                  (no frontend implementation)
```

**Layout decisions**:

- No research/data-model/contracts folders — decisions frozen in brief Q1–Q8.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/constitution/production code.
- Exact non-default Compose project name string and volume identifiers are chosen at implementation within Q2 constraints and confirmed by Phase B recon — Quill does not invent specific strings in Phase A.
- Compose credentials: fail-closed references via operator process environment or untracked Compose channel; distinct from API user-secrets/ignored settings; no explanatory YAML comments (brief step 6 / R1–R2).
- Host API command uses existing `src/LamuFlix.Api`; host frontend uses existing `web/` (brief Approach). Exact commands/config contracts come from Wisp recon evidence; newly missing facts return to recon.
- Planned image reuse pending Phase B confirmation: Postgres `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`; RabbitMQ `rabbitmq:4-alpine` (HEAD `docker-compose.yml`; recon-DEV-457-3; Q6). Same-image durability ≠ image-upgrade certification.
- Import helper for verification: existing `scripts/Import-Library.ps1` with explicit overrides (recon §3 defaults must not be used for validation); do not invent a new script.

## Test Strategy

| Family | Required evidence |
|---|---|
| Compose topology | `docker compose -f docker-compose.daily-driver.yml config` with process-local non-secret validation environment; Postgres+RabbitMQ only; named volumes; explicit `127.0.0.1` published ports in rendered output; dedicated non-default top-level project `name`; fail-closed credential references (no empty/working committed defaults) |
| Isolation / collisions | Project/volume isolation; project-scoped destructive removal; one-entrypoint guidance; port-collision docs |
| Durability | Isolated scratch library via explicit `Import-Library.ps1` overrides (never helper/operator default); restart/recreate retaining volumes → imported movie + API readiness survive; separate retained RabbitMQ proof via isolated durable test queue + persistent test message pre/post (no application-queue consumption; mounts alone insufficient) |
| Host Ollama docs | Official installer URL/path; pull/show/list; distinct listener/model/startup diagnostics; validator vs `/health/ready` distinction; README boot-claim correction; no winget/script/service-manager recommendation; same-image durability vs unsupported image-upgrade caveat |
| Host Ollama / API prerequisites (T006) | Host Ollama with `all-minilm` and unmodified production API startup with embedding validator enabled (no stripped test host) before host-API-dependent checks; missing prerequisite evidence stays open; readiness alone insufficient |
| Host Ollama functional proof (T007) | Execute documented Windows host Ollama setup/listener/pull/show/list steps; scrubbed runtime evidence records model availability and successful real host API embedding-startup validation independently; references T006 prerequisite evidence without dropping independent proof; `/health/ready` alone is insufficient; unavailable host/Ollama → `Could not run` with reason and open acceptance |
| Playback example | Placeholder-only JSON; confirm loopback binding before LocalPlay enablement (delivered DEV-458 contract); Compose LocalPlay off; existing play endpoint |
| Runtime BS Player | Scrubbed `runtime-acceptance.md` with status + visible open + history; delivery SHA = exact DEV-457 implementation HEAD exercised (distinct from dependency SHAs / spec-PR merge); after DEV-458/DEV-459 + drift recon; or `Could not run`; pre-review scrub for paths/secrets/connection strings |
| Harness honesty | task-pipeline Phase 3 step 5 commands preserved; no blanket skip from absence of C#; scope-empty SKIPPED and disabled SKIP never PASS; property exit 2 = Opt-out only with current Conductor receipt (DEV-457 receipt 15 / T001 reconfirm); N/A and Could not run distinct; no YAML-mirroring tests |

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Gates follow **task-pipeline Phase 3 step 5**. Run **sequentially** (shared worktree `obj`/`bin`; no parallel gate scripts). Preserve configured commands and report actual applicability/outcomes — **no** blanket SKIPPED/N/A for `dotnet test`, vulnerable-packages, or format solely because no C#/project files changed.

1. If any `.cs` changed (unexpected under frozen scope): Roslyn / cyclomatic / InspectCode on changed files. Scope-empty → `SKIPPED (scope-empty)`, never PASS. Scope-empty on analyzers is not a waiver for other gates.
2. Run configured `dotnet test`; report actual result/applicability — do not manufacture scope-empty solely from docs/Compose scope.
3. Property / vulnerable-packages / format per harness. Property-test exit 2 with current Conductor opt-out receipt (DEV-457 receipt 15; reconfirm T001) is **Opt-out**; without that receipt, treat as failure. Disabled `SKIP` never PASS. `N/A` and `Could not run` remain distinct.
4. Mutation only for eligible production C# (outside frozen scope requires fresh decision); otherwise honest SKIPPED/N/A from actual eligibility — never PASS by manufacture.
5. Web/contract gates by actual applicability only — no web implementation planned; do not manufacture PASS.
6. Before Size M review: explicit scrub of runtime receipt for real paths/secrets/connection strings (T008).
7. Size M three-axis review (Sentry/Ledger/Compass) under Q8 closing bar; max two remediation rounds; unresolved above-bar findings at cap → blocked, never waiver.
8. Rebased gates before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result.

## Risks

- **Dependency not delivered**: All implementation (T002+) and BS Player proof blocked until owner Gate 1 + DEV-458/DEV-459 delivered contracts + Phase B drift recon (Q5, Q7; brief step 6).
- **Secret leakage**: Mitigated by fail-closed Compose credential references, distinct API local channels, Q2/Q4 scrub rules, placeholder-only examples, and pre-review receipt scrub.
- **Entrypoint confusion / port collisions**: Mitigated by README one-entrypoint, collision docs, and project-scoped destructive reset (Q2).
- **Unqualified host bindings**: Mitigated by requiring explicit `127.0.0.1` rendered mappings (Q2; brief step 6).
- **False Ollama readiness**: Mitigated by forbidding `/health/ready` as Ollama probe, requiring production validator-enabled API boot (no stripped test host), and distinct listener/model/startup diagnostics (Q3).
- **Durability claimed from mounts alone**: Mitigated by scratch-library import proof + isolated durable queue/message broker proof (Q2, Q7).
- **Scope creep into AppHost/full Compose/C#/Dependabot**: Mitigated by Q6 file boundary and FR-014–FR-016; Dependabot gap returns to Patron tracker, not file expansion.
- **Image drift / upgrade confusion**: Noted, no ticket; Phase B recon confirms current pins; README caveat that same-image restart proof does not certify image upgrades; retain Q6 `rabbitmq:4-alpine` (Q3, Q6).

## Ordering

Matches brief Task ordering 1→6, user stories US1→US3, and Q7/step 6 hard gate: **plan freeze → Patron gate1: provisional → Rigger spec PR → user Gate 1 merge → Keel append-only T000 → T001 pickup (delivered DEV-458/DEV-459 + Dependabot coverage recon + propertyTests reconfirm) → only then T002 onward**. US3 runtime proof additionally waits on T006 prerequisite evidence and remains independently mandatory. Gauge durability checks follow Compose+README implementation; harness/review last with T008 scrub before review.

## ADR status

No new ADR required (Q8). Quill does not write ADRs.
