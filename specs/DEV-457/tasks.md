# Tasks: Windows daily-driver setup (host API + durable container services)

**Input**: `specs/DEV-457/` spec.md, plan.md, grill-closed brief.md (Q1–Q8), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-457, recon-DEV-457-2, note DEV-457, specs/PRODUCT.md

**Status**: PLAN FROZEN by Keel after independent Phase 2 step 6 read-back (2026-11-07); repairs R1–R9 verified. Ordinary owner Gate 1 remains closed; no implementation clearance. No open Phase A needs-decision or needs-recon.

**Gate 1**: No owner structural checkbox (brief Q8; PRODUCT.md §5). Handoff: **plan freeze → Patron gate1: provisional → Rigger opens the spec PR → user merges Gate 1 → Keel append-only T000** records the verified merge receipt in `specs/DEV-457/CONCLUSIONS.md` without altering rulings (single writer). Patron provisional is not owner Gate 1.

**Tests**: No invented unit/property tests for prose/config; no tests mirroring YAML (Q7). Gauge Compose/durability validation + separate manual runtime receipt. Conductor records `propertyTests: opt-out — no new domain invariants in docs/config changes` in the task note (current note DEV-457 receipt 15; reconfirm at T001).

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared build outputs (gates) are never `[P]`.

**Conventions**: No committed secrets/connection strings; fail-closed Compose credential references only (no empty/working committed defaults or authenticating placeholders); placeholders only in README/API local channels; Compose LocalPlay remains off; never lower harness thresholds; never edit AppHost/full Compose/C#/web/`dependabot.yml` under frozen scope; never prefill `runtime-acceptance.md` as PASS; no explanatory YAML comments. Exact Compose project-name string and volume identifiers chosen at implementation within Q2 constraints after T001 recon — do not invent in Phase A.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch. They are **not** canonical `task-pipeline` phases.

---

## Local phase dependency / execution order

```text
Phase 0   T000 (Keel Gate 1 record) — blocks T001+
Phase 1   T001 (Wisp drift recon + dependency receipts; Keel reanalysis; Gauge baseline; Conductor propertyTests opt-out note) — blocks T002+
Phase 2   T002 → T003   (Anvil; US1 daily-driver Compose)
Phase 3   T004 → T005   (Anvil; US2 README runbook + Ollama correction + Playback example)
Phase 4   T006          (Gauge; Compose rendering + durability proofs)
Phase 5   T007          (Gauge/operator; US3 host Ollama + BS Player runtime-acceptance — after DEV-458/DEV-459 delivered)
Phase 6   T008 → T011   (Gauge harness — sequential; honest SKIPPED/SKIP/N/A)
Phase 7   T012          (canonical Phase 4 Size M review)
Phase 8   T013 → T014   (canonical Phase 5 delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [X] T000 **Keel** appends the verified Gate 1 spec-PR merge receipt for DEV-457 to `specs/DEV-457/CONCLUSIONS.md` (append-only; single writer; preserves all prior Patron rulings). Receipt only — does not create a new ruling or rewrite history. T001–T014 MUST NOT proceed until this receipt exists (Q7–Q8; FR-018; brief step 6).

---

## Phase 1: Pickup — drift recon, dependency receipts, baseline, propertyTests opt-out (blocks implementation)

**Purpose**: Confirm Gate-1-merged brief/spec/plan/tasks still match landed main; confirm delivered DEV-458/DEV-459; confirm image/data-mount/config/startup contracts; record propertyTests opt-out.
**Checkpoint**: Fresh recon recorded; dependency delivery evidenced; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. Baseline captured. Opt-out noted.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for authorised surfaces (`README.md`, root Compose files, `docker-compose.yml` image pins for Postgres/RabbitMQ reuse confirmation, `.env.example` / `docker/api.settings.example.json` as untouched references, `src/LamuFlix.Api` host-run facts, `src/LamuFlix.AppHost/README.md` Ollama prerequisite facts, Ollama validator/health-check facts, `scripts/Import-Library.ps1`, Playback options/`Features:LocalPlay` / play endpoint facts, and DEV-458/DEV-459 delivery SHAs or receipts). Confirm delivered dependency contracts — plans or provisional specs alone do not prove delivery (Q5, Q7). Confirm current Postgres/RabbitMQ image pins for reuse (Q6; preserve `rabbitmq:4-alpine` under Q6). Record whether the new Compose filename `docker-compose.daily-driver.yml` has Dependabot coverage; assert **no** current coverage fact in Phase A — a proven gap returns to Patron for tracker disposition without editing `dependabot.yml` or expanding infrastructure files here (FR-015; brief step 6). Reconfirm Conductor propertyTests opt-out receipt on note `DEV-457` (receipt 15 currently records it; T010 cites the current receipt). **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main. Amend tasks only where authorized by demonstrated drift. **STOP** on unresolved drift, missing dependency delivery, or missing contract facts. **Gauge** records pre-edit baseline. **Conductor** records/reconfirms `propertyTests: opt-out — no new domain invariants in docs/config changes` on note `DEV-457` (Q7; FR-017). No production edits in this task. Owner Gate 1 merge + delivered DEV-458/DEV-459 + this T001 pickup MUST precede T002 onward (Q7).

---

## Phase 2: Daily-driver Compose entrypoint (Anvil; Priority: P1 — User Story 1)

**Goal**: New root `docker-compose.daily-driver.yml` with durable Postgres/RabbitMQ only.
**Independent Test**: File renders with locally supplied non-secret values; topology matches FR-001–FR-002.
**Order**: T002 → T003.

- [X] T002 [US1] After T000 + T001 (Gate 1 merge, delivered DEV-458/DEV-459, pickup recon): add `docker-compose.daily-driver.yml` at repo root providing Postgres and RabbitMQ only. Require a top-level dedicated non-default Compose project `name` (exact string chosen within Q2 using T001 evidence — do not reuse the default project name; exact identifiers remain deferred). Publish ports with explicit `127.0.0.1` host bindings (not unqualified host-port mappings). Use named volumes with project/volume isolation. Credentials MUST be fail-closed Compose credential references supplied via operator process environment or an untracked local Compose configuration channel — no empty/working committed defaults, fallbacks, or authenticating placeholders; no new tracked template. That Compose channel is distinct from API user-secrets / ignored local API settings. Reuse current Postgres/RabbitMQ image pins confirmed by T001. No explanatory YAML comments. Do **not** edit `docker-compose.yml`, AppHost, `dependabot.yml`, or add api/web/ollama services to this file (FR-001, FR-002, FR-015, FR-016; Q1–Q2, Q6; SC-001; brief step 6)
- [X] T003 [US1] Render and verify with `docker compose -f docker-compose.daily-driver.yml config` using process-local non-secret validation environment values: confirm rendered output shows only Postgres+RabbitMQ, named volumes, explicit `127.0.0.1` host bindings (reject unqualified mappings), and the dedicated non-default project name. Reject committed real credentials/connection strings, empty/working defaults, and authenticating placeholders/fallback secrets. Confirm project/volume isolation guidance and that destructive volume removal is project-scoped (never routine startup). Confirm full Compose and AppHost files remain untouched in the task diff (FR-002, FR-014, FR-016; Q2, Q6, Q7)

---

## Phase 3: README daily-driver runbook (Anvil; Priority: P1 — User Story 2)

**Goal**: Complete Windows daily-driver section in `README.md` only — Ollama, host startup, Playback example, stop/troubleshoot, corrected boot claim.
**Independent Test**: Doc review against Q3–Q4/Q6 and recon-DEV-457-2 §12; placeholders only.
**Order**: T004 → T005 (same file — sequential).

- [ ] T004 [US2] Edit `README.md` only: add section title **Windows daily-driver setup** with order prerequisites → configure → start → verify → stop → troubleshoot (`ASSUMPTIONS.md`); document dedicated Compose project usage and project-scoped destructive reset; host API via existing `src/LamuFlix.Api`; host frontend via existing `web/`; distinguish Compose operator environment / untracked Compose config channel from API user-secrets or ignored local development settings; ordinary stop/restart/`down` without `-v` vs destructive volume removal; port-collision and one-entrypoint-at-a-time guidance; troubleshooting caveat that same-image restart/recreate durability proof does **not** certify broker image upgrades (retain Q6 `rabbitmq:4-alpine`; digest change not silently authorised) (FR-003, FR-007, FR-015; Q2, Q4, Q6; ASSUMPTIONS; brief step 6)
- [ ] T005 [US2] In the same `README.md` section: document official Windows Ollama path (`https://ollama.com/download/OllamaSetup.exe`, background app, `http://localhost:11434`, `ollama pull/show/list all-minilm`); document real embedding startup validation and distinct listener / model / startup-failure diagnostics; correct the claim that Ollama never blocks API boot; state that `/health/ready` is not the Ollama probe; forbid substituting compose `exec` host steps; do **not** recommend winget, downloaded install scripts, or new service managers; add illustrative `Playback:Players` JSON with `Name`, `ExecutablePath` placeholder, operator-selected `Formats`, and explicit loopback-host LocalPlay opt-in only — instruct operators to confirm loopback binding **before** LocalPlay enablement, relying on the delivered DEV-458 refusal contract (no claim that DEV-458 fails open); no real paths/secrets/enabled shipped defaults; state Compose LocalPlay remains off and the existing gated play endpoint is used unchanged; no new runbook file; no runtime code changes (FR-005–FR-009, FR-016; Q3–Q4, Q6; recon-DEV-457 §§5/7; recon-DEV-457-2 §12; SC-003–SC-004; brief step 6)

---

## Phase 4: Gauge Compose and durability validation (Priority: P1 — User Story 1 evidence)

**Goal**: Prove rendering, isolation, readiness, and retained application/broker state.
**Independent Test**: Gauge evidence recorded separately from manual BS Player receipt.

- [ ] T006 [US1] **Gauge** validates daily-driver Compose via `docker compose -f docker-compose.daily-driver.yml config` with process-local non-secret validation environment: rendered Postgres+RabbitMQ only; named-volume mounts; explicit `127.0.0.1` host bindings (reject unqualified mappings); dedicated non-default project name; project/volume isolation and collision behaviour vs guidance. Durability: import into an **isolated scratch library** using existing `scripts/Import-Library.ps1` with **explicit** `-LibraryRoot` / `-ApiBase` (and any other helper parameters) overrides — never the operator/helper default library path (recon §3). Restart/recreate retaining volumes must prove the imported movie and API readiness survive. Separately prove retained RabbitMQ broker state with an **isolated durable test queue** and a **persistent test message** established before restart/recreate and observed after, without consuming application queues — volume/mount existence alone is insufficient and acceptance-blocking if missing (FR-004; Q2). **Prerequisites before host-API-dependent checks**: host Ollama available with `all-minilm`, and the unmodified production API startup path with embedding validator enabled (no validator-stripped test host; recon §8 / supplement §12.3). Record distinct listener / model / startup diagnostics as prerequisite evidence; reference that evidence from T007 without dropping T007’s independent proof. Missing prerequisite evidence leaves T006 open — readiness alone must not close it. Record results with honest PASS/failure/`Could not run` (FR-004, FR-018; Q2, Q3, Q7; SC-002; brief step 6)

---

## Phase 5: Host Ollama + BS Player runtime acceptance (Priority: P1 — User Story 3)

**Goal**: Scrubbed real-host proof of documented Windows Ollama setup/verification and BS Player imported-movie open after delivered dependencies.
**Independent Test**: `specs/DEV-457/runtime-acceptance.md` with independently recorded Ollama model-visibility + real API embedding-startup fields and BS Player fields, or `Could not run`.
**Prerequisite**: T001 confirmed DEV-458 and DEV-459 delivered.

- [ ] T007 [US3] After T001 dependency receipts and T004–T006 docs/infra evidence (including T006 prerequisite evidence for host Ollama/`all-minilm` and production API validator boot — referenced here, not a substitute for independent T007 proof): on a real Windows host, execute the documented Windows host Ollama setup/verification sequence (official installer path as applicable, background listener at `http://localhost:11434`, `ollama pull/show/list all-minilm` as applicable); record independently in scrubbed `specs/DEV-457/runtime-acceptance.md` (1) model availability via show/list after pull as applicable and (2) successful real host API embedding-startup validation — `/health/ready` alone is insufficient and must not close the Ollama proof; then import a chosen local movie through the existing real import path into an isolated scratch library (explicit Import-Library overrides; never operator default); confirm loopback binding **before** enabling LocalPlay, relying on the delivered DEV-458 refusal contract; enable loopback LocalPlay with locally configured BS Player (operator-discovered path; never commit it); call existing `POST /movies/{id}/play` (Compose LocalPlay remains off; existing endpoint unchanged); record date, **delivery SHA = exact DEV-457 implementation HEAD exercised by this proof** (distinct from dependency SHAs and from the spec-PR merge receipt), dependency SHAs/receipts, Windows/player versions, selected format, scrubbed config summary, returned status, visibly observed BS Player opening the resolved imported file, and playback-history evidence; redact real library/player paths, secrets, and connection strings. Automated `IProcessStarter` substitutes MUST NOT close the player proof. If host/Ollama/player/media/dependencies unavailable, record `Could not run` with reason, keep this task open, and do **not** claim ticket acceptance or Done (FR-005–FR-006, FR-010–FR-013, FR-018; Q3, Q5, Q7; SC-003, SC-005; brief step 6)

---

## Phase 6: Verification gates (Gauge) — sequential

- [ ] T008 **Gauge** confirms frozen scope only: diff limited to `README.md`, `docker-compose.daily-driver.yml`, and (when written) `specs/DEV-457/runtime-acceptance.md` plus Phase A specs already under `specs/DEV-457`; no AppHost/full-Compose/C#/web/package/schema/API/`dependabot.yml` edits. Reject authenticating placeholders/defaults in tracked Compose/examples. Before review, explicitly scrub `runtime-acceptance.md` (when present) for real library/player paths, secrets, and connection strings (FR-002, FR-011, FR-014–FR-016; Q5–Q6; SC-006; brief step 6)
- [ ] T009 **Gauge** runs applicable changed-C# Roslyn / cyclomatic / InspectCode scripts; if no production `.cs` changed, report `SKIPPED (scope-empty)` — never PASS (FR-018; Q7). Sequential with T010–T011. Scope-empty SKIPPED is not a blanket waiver for other gates.
- [ ] T010 **Gauge** runs configured `dotnet test` / property / vulnerable-packages / `dotnet format --verify-no-changes` per **task-pipeline Phase 3 step 5**; preserve commands and report actual applicability/outcomes — **no** blanket SKIPPED/N/A solely because this ticket has no C#/project-file changes (durability and runtime checks also remain mandatory). Cite the current Conductor propertyTests opt-out receipt on note `DEV-457` (receipt 15 currently; reconfirmed at T001): property-test exit 2 with that receipt is **Opt-out**, otherwise failure; do not invent a harness per-ticket toggle or edit harness. Scope-empty `SKIPPED` and disabled `SKIP` never PASS; `N/A` and `Could not run` remain distinct. Invent no prose/YAML unit or property tests (FR-017–FR-018; Q7; brief step 6). After T009; not parallel.
- [ ] T011 **Gauge** reports web/contract/mutation applicability only from actual configured results (no web implementation and no eligible production C# expected — do not manufacture PASS; mutation outside frozen scope needs a fresh decision) (FR-018; Q7; SC-006). After T010; sequential.

---

## Phase 7: Size M review (canonical Phase 4)

- [ ] T012 **Sentry / Ledger / Compass** three-axis review under brief Q8 closing bar (Critical/High block; Medium/Low explicitly dispositioned Follow-up or noted; unmet ticket acceptance or unsafe exposure blocks regardless of severity; frozen scope = Q6 files/deliverables; anything else is a follow-up issue). Max two review/fix rounds; unresolved above-bar findings at cap → blocked, never waiver. Unresolved findings return to Bernstein (FR-019; Q8; SC-006).

---

## Phase 8: Delivery (canonical Phase 5)

- [ ] T013 Rebase onto latest default branch; re-run applicable gates on rebased HEAD; open/update PR with merge-bar proof. No CHANGELOG. No CONTEXT.md glossary append required (Q8 — no new glossary concept) (FR-018; Q7–Q8)
- [ ] T014 **Conductor** hands off awaiting-merge for DEV-457; only the user merges. Ticket acceptance/Done requires T007 runtime proof success (or remains open on `Could not run`) (FR-012; Q5, Q7)

---

## Dependencies & Execution Order

- Phase 0 blocks all Build (user Gate 1 merge receipt).
- Phase 1 blocks production edits: owner Gate 1 + delivered DEV-458/DEV-459 + T001 pickup MUST precede T002 onward (Q7; brief step 6). T001 also records Dependabot coverage of the new Compose filename (no coverage fact asserted in Phase A) and reconfirms propertyTests opt-out receipt.
- Phase 2 (US1 Compose) before Phase 4 durability validation that needs the file.
- Phase 3 (US2 README) may follow or immediately trail Phase 2; T004–T005 share `README.md` — sequential; T005 orders loopback confirmation before LocalPlay enablement.
- Phase 4 Gauge durability after Compose exists; T006 host-API proofs require host Ollama/`all-minilm` and production validator boot evidence first; isolated scratch library + durable test queue/message proofs required.
- Phase 5 host Ollama + BS Player runtime after dependency delivery + docs/infra; references T006 prerequisite evidence without dropping independent T007 proof; delivery SHA = exact DEV-457 implementation HEAD exercised.
- Phases 6–8 follow task-pipeline Phase 3 step 5 gates → review → ship; T008 pre-review receipt scrub required.

### Parallel opportunities

- None required. T002 is single-file; T004–T005 same file; T008–T011 sequential shared reporting.
- **Not parallel**: T009–T011 (shared gate reporting / build outputs). T004 with T005 (same `README.md`).

### MVP

US1 Compose + US2 README is the minimum operable documentation/infra delivery; US3 runtime proof remains mandatory for ticket acceptance and may stay open as `Could not run`.

## Notes

- `[P]` tasks = different files, no incomplete deps, no shared build outputs.
- Local Phase N ≠ canonical task-pipeline phase numbers.
- Quill does not invent exact Compose project-name or volume-name strings in Phase A; T001/T002 resolve within Q2.
- Keel recorded PLAN FROZEN after independent step 6 read-back; owner Gate 1 and runtime acceptance remain pending.
