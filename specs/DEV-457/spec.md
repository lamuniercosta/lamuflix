# Feature Specification: Windows daily-driver setup (host API + durable container services)

**Feature Branch**: `feature/457-spec`
**Created**: 2026-11-07
**Status**: gate1: provisional — PLAN FROZEN by Keel after independent Phase 2 step 6 repair read-back (2026-11-07). R1–R9 verified. Owner Gate 1 remains closed. No implementation clearance.
**Input**: Ticket DEV-457; grill-closed `brief.md` (Q1–Q8) including Phase 2 step 6 refinements; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-457`, `recon-DEV-457`, `recon-DEV-457-2`, `recon-DEV-457-3`, `adjudication-DEV-457-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependencies: DEV-458, DEV-459. Grill commit: `94657cd8`. Patron step 6 ruling: `1bf06eac`; brief refinements: `c946b981`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Q8; `PRODUCT.md` §5). Handoff after repairs: plan freeze → Patron gate1: provisional → Rigger spec PR → user Gate 1 merge. Ordinary owner Gate 1 on the spec PR remains pending. Patron provisional is not owner approval.

## Clarifications

### Session 2026-11-07

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity? → A: None after Q1–Q8 for topology, durability/isolation/secrets, host Ollama docs and README correction, Playback example and LocalPlay exercise boundary, BS Player acceptance sequencing, file/charter/editorial boundary, task order and verification, and loop terms. Official Windows Ollama install facts are supplied by `recon-DEV-457-2` §12.2–12.3. No residual drafting `needs decision` or `needs recon`.

## Scope

**In scope** (`brief.md` Q1–Q8; ticket Scope via recon-DEV-457 §1):

1. **Dedicated daily-driver Compose entrypoint**: New root `docker-compose.daily-driver.yml` supplying durable Postgres and RabbitMQ only, with named volumes and loopback-published ports. Windows host runs API, host Ollama, and host Vite. Existing full Compose (`docker-compose.yml`) and disposable AppHost stay untouched (Q1, Q6).
2. **Isolation, durability, and secrets**: Dedicated non-default top-level Compose project name; explicit `127.0.0.1` published port bindings; operator-configurable ports; fail-closed Compose credential references via operator process environment or an untracked local Compose configuration channel (distinct from API user-secrets / ignored API settings); no empty/working committed defaults, authenticating placeholders, real credentials, or connection strings; no new tracked template; no explanatory YAML comments. Verification proves an imported movie (isolated scratch library with explicit Import-Library overrides) and API readiness survive service restart/recreate with volumes retained, and separately proves retained RabbitMQ broker state via an isolated durable test queue and persistent test message (volume existence alone is insufficient). Document collisions, one entrypoint at a time, non-destructive stop/down without `-v`, and project-scoped destructive volume removal separately (Q2; brief step 6).
3. **Host Ollama documentation and README correction**: Document Windows host Ollama install via official `OllamaSetup.exe`, listener at `http://localhost:11434`, `all-minilm` pull/show/list, startup ordering, real embedding validation and distinct listener/model/startup diagnostics. Correct the README claim that Ollama never blocks API boot; `/health/ready` is not the Ollama probe. No runtime behaviour changes. Keep current image pins and fixture unchanged; existing image-pin drift noted, no ticket; README troubleshooting notes that same-image restart/recreate proof does not certify image upgrades (Q3, Q6; recon §4; supplement §12; brief step 6).
4. **Runbook and Playback example in README only**: Full Windows daily-driver runbook and illustrative `Playback:Players` JSON in `README.md` only (section title and order per `ASSUMPTIONS.md`). Example uses explicit loopback-host LocalPlay opt-in only after loopback binding is confirmed (delivered DEV-458 contract) and player entries with `Name`, `ExecutablePath` placeholder, and operator-selected `Formats`. No real paths, credentials, or enabled shipped defaults. Compose credentials use the Compose channel above; API local values go to user-secrets or ignored local development settings. Compose LocalPlay remains off. Manual exercise uses the existing gated play endpoint; no new launcher/shell logic or code/API changes (Q4; brief step 6).
5. **Real BS Player acceptance after dependencies**: After owner Gate 1, delivered DEV-458 and DEV-459 contracts, and Phase B drift recon, import a local movie through the existing real import path into an isolated scratch library, confirm loopback before enabling LocalPlay with locally configured BS Player, call existing `POST /movies/{id}/play`, and record returned status, visibly observed BS Player opening the resolved imported file, and playback-history evidence in scrubbed `specs/DEV-457/runtime-acceptance.md`. Delivery SHA means the exact DEV-457 implementation HEAD exercised, distinct from dependency SHAs and the spec-PR merge receipt. Automated `IProcessStarter` substitutes cannot close this proof. Missing host/player/media/dependencies → `Could not run` with reason; runtime task stays open; no ticket acceptance or Done claim (Q5; brief step 6).
6. **Bounded implementation files**: `README.md`, new `docker-compose.daily-driver.yml`, and Phase B `specs/DEV-457/runtime-acceptance.md` only. Phase A artifacts stay under `specs/DEV-457`. Reuse current Postgres/RabbitMQ image choices subject to Phase B recon confirmation (`rabbitmq:4-alpine` preserved). Dependabot coverage of the new Compose filename is recorded at T001 without asserting a Phase A coverage fact; a proven gap returns to Patron for tracker disposition without editing Dependabot configuration here. No packages, projects/layers, schema/API/status/DTO change, frontend code, AppHost/full-Compose/Dependabot edit, launcher rewrite, or implementation of dependency tickets (Q6; brief step 6).

**Out of scope**:

- Changes to `docker-compose.yml`, `.env.example`, `docker/api.settings.example.json`, AppHost files, C#, web code, or any new script/tracked settings template.
- New NuGet/npm dependency, project, top-level folder beyond the authorised new Compose file, architectural layer, schema change, or public API shape change.
- Runtime behaviour changes to Ollama startup validation, LocalPlay gating, or `Process.Start` / launcher code.
- winget / downloaded-script / new service-manager Ollama recommendations; inventing install commands beyond recon-DEV-457-2 §12.
- Prefilling `runtime-acceptance.md` as passing during Phase A.
- Inventing unit/property tests for prose/config or tests that mirror YAML.
- Image-pin drift remediation (noted, no ticket).
- Anything outside Q6 files and Q1–Q5 deliverables is a follow-up issue, not a finding in this round (Q8).

**Precedence**: Ticket Scope & Technical Design is already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern boundaries. Taste in `ASSUMPTIONS.md`. Ordinary owner Gate 1 remains pending. Quill invents no decisions.

## Domain vocabulary *(mandatory — glossary terms for CONTEXT.md at delivery)*

No new glossary concept is required (brief Q8). Consume existing operational terms without rewriting unrelated domain vocabulary:

**Windows daily-driver host**
Constitutional setup where the API runs as a Windows host process while Postgres and RabbitMQ run in containers; LocalPlay is off in Compose (constitution 709–710).
_Avoid_: AppHost disposable stack, full Compose all-in-one path (when meaning this host topology)

**Daily-driver Compose entrypoint**
The dedicated root Compose file that publishes durable loopback Postgres/RabbitMQ for the Windows host API, separate from full Compose and AppHost.
_Avoid_: default Compose project, AppHost resources

Consumed vocabulary (already in use; not redefined here): LocalPlay, Playback:Players, Ollama embedding model / `all-minilm`, `/health/ready`, PlaybackEvent / playback history.

## What the User Will See *(mandatory)*

Nothing visible: this ticket documents and verifies the Windows daily-driver operator path (host API + durable container services + host Ollama + LocalPlay proof). The default plain `docker compose up` stack on main is unchanged; no new UI controls or pages are delivered.

Operator-facing outcomes (documentation and dedicated entrypoint, not default-Compose UI):

- **Where**: Root `README.md` section **Windows daily-driver setup** (`ASSUMPTIONS.md`); new root `docker-compose.daily-driver.yml`.
- **What the operator sees or does**: Follow prerequisites → configure → start → verify → stop → troubleshoot; start durable Postgres/RabbitMQ via the dedicated entrypoint; run host API and host Vite; install/verify host Ollama; optionally enable loopback LocalPlay with a placeholder Playback example; after DEV-458/DEV-459, prove BS Player opens an imported movie and record scrubbed evidence.
- **What it needs to show up**: Docker available for the daily-driver Compose project; operator-supplied local credentials/ports/library root/player path; host Ollama with `all-minilm`; delivered DEV-458/DEV-459 for the real player proof.

### Smoke Test

Skipped — nothing visible on the default Compose stack. Runtime BS Player / durability proofs are manual Phase B acceptance (`runtime-acceptance.md` and Gauge Compose checks), not e2e smoke against plain `docker compose up`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Durable daily-driver infrastructure (Priority: P1)

An operator on Windows starts only Postgres and RabbitMQ from the dedicated daily-driver Compose file. Services use named volumes, bind to loopback-published ports, use a dedicated non-default Compose project name, and take credentials/ports from operator-supplied local configuration. Existing full Compose and AppHost remain separate and unmodified.

**Why this priority**: Ticket core — host API with durable container-backed services; neither existing entrypoint satisfies this (Q1–Q2; recon §§2–3).

**Independent Test**: `docker compose -f docker-compose.daily-driver.yml config` renders with process-local non-secret validation values; named-volume mounts and explicit `127.0.0.1` bindings appear in rendered output; project isolation and collision docs are present; restart/recreate with volumes retained preserves imported movie (scratch library) + API readiness and separately retained broker state via durable test queue/message (Gauge; Q2, Q7).

**Acceptance Scenarios**:

1. **Given** operator-supplied non-secret local credentials and ports via environment or untracked Compose config, **When** the daily-driver Compose file is rendered/started, **Then** only Postgres and RabbitMQ start, with named volumes, explicit `127.0.0.1` published ports, fail-closed credential references (no empty/working committed defaults), and a dedicated non-default top-level project name.
2. **Given** a movie imported into an isolated scratch library (explicit Import-Library overrides) and a healthy host API against those services, **When** the service containers are restarted/recreated while named volumes are retained, **Then** the imported movie and API readiness still survive.
3. **Given** an isolated durable RabbitMQ test queue with a persistent test message established before restart/recreate with volumes retained (application queues not consumed), **When** services return, **Then** that message/queue state is still observable (volume existence alone is not sufficient proof).
4. **Given** ordinary stop or `down` without `-v`, **When** the operator follows the README, **Then** volumes are retained; project-scoped destructive volume removal is documented separately and is never part of routine startup.
5. **Given** full Compose or AppHost already running, **When** the operator reads collision / one-entrypoint guidance, **Then** the README states one entrypoint at a time and how to handle port collisions.

---

### User Story 2 - Host Ollama and daily-driver runbook in README (Priority: P1)

The README is the sole operator runbook. It documents Windows Ollama install via official `OllamaSetup.exe`, background app listener at `http://localhost:11434`, `ollama pull/show/list all-minilm`, startup ordering (model available before host API boot), real embedding-validator diagnostics (not `/health/ready` as the Ollama probe), and corrects the claim that Ollama never blocks API boot — without changing runtime behaviour. It also documents host API (`src/LamuFlix.Api`) and host frontend (`web`) startup, local configuration via user-secrets or ignored local development settings, and an illustrative Playback JSON with placeholders only.

**Why this priority**: Ticket Ollama docs + Playback example; README is the authorised sole location (Q3–Q4, Q6).

**Independent Test**: Doc review against Q3–Q4/Q6 and recon-DEV-457-2 §12; no invented winget/script/service-manager path; placeholders only; Compose LocalPlay remains off in tracked examples.

**Acceptance Scenarios**:

1. **Given** a Windows operator without host Ollama, **When** they follow the README, **Then** they install via official `OllamaSetup.exe` from the cited download URL, open the background app, pull/show/list `all-minilm`, and only then start the host API.
2. **Given** unreachable listener, absent model, or startup validation failure, **When** they follow diagnostics, **Then** those cases are distinguished; `/health/ready` is not presented as the Ollama probe; compose `docker compose exec …` steps are not substituted for host commands.
3. **Given** the prior README claim that Ollama never blocks API boot, **When** the corrected README is read, **Then** it matches the real startup validator behaviour without changing runtime code.
4. **Given** the illustrative Playback example, **When** an operator configures LocalPlay, **Then** they confirm loopback binding before enablement (delivered DEV-458 contract), see explicit loopback-host opt-in, `Playback:Players` entries with `Name`, `ExecutablePath` placeholder, and operator-selected `Formats`, with no real paths/secrets/enabled shipped defaults; Compose LocalPlay stays off.
5. **Given** the runbook structure, **When** the README section is read, **Then** it follows prerequisites → configure → start → verify → stop → troubleshoot under title **Windows daily-driver setup** (`ASSUMPTIONS.md`).

---

### User Story 3 - Real BS Player imported-movie proof (Priority: P1)

After owner Gate 1, delivered DEV-458 and DEV-459, and Phase B exact-head drift recon, the operator imports a chosen local movie through the existing real import path into an isolated scratch library, confirms loopback binding before enabling LocalPlay with locally configured BS Player, calls existing `POST /movies/{id}/play`, and records returned status, visibly observed BS Player opening the resolved imported file, and playback-history evidence in scrubbed `specs/DEV-457/runtime-acceptance.md` (date, delivery SHA = exact DEV-457 implementation HEAD exercised — distinct from dependency SHAs and spec-PR merge — dependency SHAs/receipts, Windows/player versions, selected format, scrubbed config summary). Real library/player paths, secrets, and connection strings are redacted before review. Automated launcher substitutes cannot close the proof. Unavailable prerequisites yield `Could not run` with reason and keep the runtime task open.

**Why this priority**: Ticket acceptance criterion; depends on DEV-458/DEV-459 delivered contracts (Q5, Q7).

**Independent Test**: Scrubbed `runtime-acceptance.md` with independent criteria recorded; or explicit `Could not run` — never PASS by automation alone.

**Acceptance Scenarios**:

1. **Given** delivered DEV-458/DEV-459 and Phase B drift recon confirming shipped contracts, **When** the operator completes scratch-library import + loopback confirmation + LocalPlay + `POST /movies/{id}/play`, **Then** the receipt records returned status, visible BS Player open of the resolved imported file, playback-history evidence, and delivery SHA as the exact DEV-457 implementation HEAD exercised.
2. **Given** missing host, player, media, or dependencies, **When** the proof is attempted, **Then** the result is `Could not run` with reason, the runtime task stays open, and ticket acceptance/Done is not claimed.
3. **Given** a written receipt, **When** it is reviewed, **Then** real library/player paths, secrets, and connection strings are redacted and dependency SHAs/receipts are present separately from the delivery SHA.

### Edge Cases

- Port collision with another entrypoint or local service → documented; one entrypoint at a time (Q2).
- Destructive `down -v` / volume removal mistaken for ordinary stop → README separates non-destructive vs destructive paths (Q2).
- Treating `/health/ready` or `ollama list` alone as proof of functional host Ollama → forbidden; real API startup validator proof required (Q3; recon §4).
- Substituting compose-only `docker compose exec ollama …` for host Ollama steps → forbidden (supplement §12.4).
- Prefilling runtime acceptance as PASS in Phase A → forbidden (Q5–Q6).
- Claiming durability from named-volume mounts alone without retained application/broker state → insufficient (Q2, Q7).
- Image-pin drift across compose/fixture pins → noted, no ticket; leave pins unchanged (Q3, Q6).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Delivery MUST add root `docker-compose.daily-driver.yml` providing durable Postgres and RabbitMQ only, with named volumes, explicit `127.0.0.1` published ports, and a dedicated non-default top-level Compose project `name`; no explanatory YAML comments; Windows host runs API, Ollama, and Vite; existing full Compose and AppHost MUST remain untouched (Q1, Q6; brief step 6).
- **FR-002**: Daily-driver Compose credentials MUST be fail-closed references supplied via operator process environment or an untracked local Compose configuration channel (distinct from API user-secrets / ignored API settings); ports, library root, and player executable MUST be operator-supplied and untracked; no empty/working committed defaults, authenticating placeholders, real credentials, or connection strings MAY be committed; no new tracked template (Q2; constitution 444; brief step 6).
- **FR-003**: README MUST document ordinary stop/restart and `down` without `-v` separately from project-scoped destructive volume removal; MUST document port-collision handling and one entrypoint at a time (Q2; brief step 6).
- **FR-004**: Durability acceptance MUST prove imported movie (isolated scratch library with explicit Import-Library parameter overrides; never operator/helper default) and API readiness survive restart/recreate with volumes retained, and MUST separately prove retained RabbitMQ broker state via an isolated durable test queue and persistent test message observed before and after without consuming application queues; volume existence alone is insufficient (Q2, Q7; brief step 6).
- **FR-005**: README MUST document Windows host Ollama using official `OllamaSetup.exe` from `https://ollama.com/download/OllamaSetup.exe`, background app listener at `http://localhost:11434`, then `ollama pull all-minilm`, `ollama show all-minilm`, and `ollama list`/`ls` before real host API startup; MUST NOT recommend winget, executing downloaded install scripts, or new service managers (Q3, Q6; recon-DEV-457-2 §12.2–12.3).
- **FR-006**: README MUST document real embedding startup validation and distinct listener/model/startup-failure diagnostics; MUST NOT present `/health/ready` as the Ollama probe; MUST correct the claim that Ollama never blocks API boot; MUST NOT change runtime behaviour; host-API-dependent validation MUST use the unmodified production API startup path with embedding validator enabled (no validator-stripped test host) (Q3; recon §§4/8; brief step 6).
- **FR-007**: README MUST be the sole location for the complete Windows daily-driver runbook and illustrative Playback JSON; no new runbook file or tracked settings template is authorised (Q4, Q6).
- **FR-008**: Illustrative Playback example MUST show `Features:LocalPlay` true only for explicit loopback host configuration after loopback binding is confirmed (delivered DEV-458 contract) and `Playback:Players` entries with `Name`, `ExecutablePath` placeholder, and operator-selected `Formats`; MUST NOT ship enabled defaults, real paths, or secrets; Compose LocalPlay MUST remain off (Q4; constitution 446–449, 538; brief step 6).
- **FR-009**: Manual LocalPlay exercise MUST use the existing gated play endpoint only; this ticket MUST NOT add launcher/shell logic or change code/API/LocalPlay runtime behaviour (Q4–Q5).
- **FR-010**: After Gate 1 and delivered DEV-458/DEV-459 plus Phase B drift recon, BS Player proof MUST import via the existing real import path into an isolated scratch library, confirm loopback before LocalPlay enablement, call existing `POST /movies/{id}/play`, and record returned status, visible player open of the resolved imported file, and playback-history evidence in scrubbed `specs/DEV-457/runtime-acceptance.md` (Q5; brief step 6).
- **FR-011**: `runtime-acceptance.md` MUST include date, delivery SHA (exact DEV-457 implementation HEAD exercised — distinct from dependency SHAs and from the spec-PR merge receipt), dependency SHAs/receipts, Windows/player versions, selected format, and scrubbed config summary; MUST redact real library/player paths, secrets, and connection strings before review; MUST NOT be prefilled as passing in Phase A (Q5–Q6; brief step 6).
- **FR-012**: Unavailable host/player/media/dependencies MUST be recorded as `Could not run` with reason; runtime task stays open; ticket acceptance/Done MUST NOT be claimed (Q5, Q7).
- **FR-013**: Automated tests that substitute `IProcessStarter` MUST NOT close BS Player acceptance (Q5).
- **FR-014**: Implementation file set MUST be limited to `README.md`, `docker-compose.daily-driver.yml`, and `specs/DEV-457/runtime-acceptance.md`; Phase A records stay under `specs/DEV-457` (Q6).
- **FR-015**: Postgres/RabbitMQ image choices MUST reuse current pins subject to Phase B recon confirmation (`rabbitmq:4-alpine` preserved); image-pin drift is noted with no ticket; compose fixture and existing pins stay unchanged by this ticket’s Ollama docs work; README MUST note that same-image restart/recreate durability does not certify image upgrades; Dependabot coverage of the new Compose filename is recorded at T001 without a Phase A coverage assertion, and a proven gap returns to Patron for tracker disposition without editing Dependabot configuration here (Q3, Q6; brief step 6).
- **FR-016**: No new packages, projects/layers, schema/API/status/DTO changes, frontend code, AppHost/full-Compose/Dependabot edits, launcher rewrites, or dependency-ticket implementations (Q6; charter §2.3 items 1/3/4 none).
- **FR-017**: Verification MUST separate automated/static evidence from manual runtime acceptance; invent no unit/property tests for prose/config and no tests mirroring YAML; Conductor records `propertyTests: opt-out — no new domain invariants in docs/config changes` in the task note (current DEV-457 receipt 15; reconfirm at T001) (Q7; brief step 6).
- **FR-018**: Harness gates follow task-pipeline Phase 3 step 5 with honest `PASS` / failure / `SKIPPED (scope-empty)` / `SKIP` / `N/A` / `Could not run` / Opt-out; no blanket skip of configured `dotnet test`, vulnerable-packages, or format solely because no C# changed; scope-empty exit 2 and disabled SKIP are never PASS; property exit 2 is Opt-out only with the current Conductor receipt, otherwise failure; applicable failures and Could not run block; runtime Could not run leaves acceptance open (Q7; brief step 6).
- **FR-019**: Review uses the Q8 closing bar (Critical/High block; Medium/Low explicitly dispositioned; unmet acceptance or unsafe exposure blocks regardless of severity), frozen scope to Q6 files/deliverables, and two-round cap; unresolved above-bar findings at cap mean blocked, never waiver (Q8).

### Key Entities

- **Daily-driver Compose project**: Isolated non-default Compose project hosting durable Postgres and RabbitMQ with named volumes and loopback ports.
- **Host Ollama prerequisite**: Local Windows Ollama at `http://localhost:11434` with `all-minilm`, required for real host API startup validation.
- **Playback player mapping**: Configuration-only `Playback:Players` entries (Name, ExecutablePath, Formats); never a database table.
- **Runtime acceptance receipt**: Scrubbed Phase B evidence file under `specs/DEV-457/runtime-acceptance.md`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator can start durable Postgres/RabbitMQ from the dedicated entrypoint (explicit `127.0.0.1` bindings, fail-closed credential references, non-default project name) and connect a host API using only documented local configuration (no committed secrets).
- **SC-002**: After restart/recreate with volumes retained, the previously imported scratch-library movie and API readiness still succeed, and RabbitMQ broker state retention is evidenced by isolated durable queue/message observation separately from volume mounts.
- **SC-003**: README documents official Windows Ollama install/pull/verify steps, distinct diagnostics, and corrected startup-validator semantics without runtime code changes.
- **SC-004**: README supplies a placeholder-only Playback example with loopback confirmation before LocalPlay opt-in and Compose LocalPlay remaining off.
- **SC-005**: After DEV-458/DEV-459 delivery and Phase B recon, scrubbed `runtime-acceptance.md` records BS Player visible-open + history proof with delivery SHA = exact DEV-457 implementation HEAD exercised, or explicit `Could not run` with reason — never silent PASS.
- **SC-006**: Harness gates and review report honest statuses under task-pipeline Phase 3 step 5 and the Q8 bar; no fabricated PASS for scope-empty or inapplicable gates; no blanket C#-absence skip; no unit/property tests invented for prose/YAML.

## Assumptions

- Taste assumptions already logged in `specs/DEV-457/ASSUMPTIONS.md` (README section title **Windows daily-driver setup**; runbook order prerequisites → configure → start → verify → stop → troubleshoot).
- Exact non-default Compose project name string and exact named-volume identifiers are implementation choices constrained by Q2 (dedicated non-default project; durable named volumes) and confirmed at Phase B — Quill does not invent specific strings here.
- Exact loopback port numbers and credentials remain operator-configurable local values (Q2).
- Current Postgres/RabbitMQ image pins at drafting HEAD are the planned reuse set pending Phase B recon confirmation (Q6): Postgres `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`; RabbitMQ `rabbitmq:4-alpine` (recon/HEAD `docker-compose.yml`).
- No Gherkin/acceptance-test opt-in requested for this ticket.
- Dependencies DEV-458 and DEV-459 must be delivered (not merely planned) before BS Player runtime acceptance (Q5, Q7).
