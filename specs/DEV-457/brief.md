# DEV-457 — Phase A grill brief

Status: grill closed with Patron at Q8 of maximum 12. Scope settled for drafting; plan is not yet frozen. Gate 1 remains closed. No implementation or runtime acceptance has run.

Worktree: F:/Dev/LamuFlix.worktrees/feature-457-spec. Branch: feature/457-spec. Original recon base: 659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3. Supplement: recon-DEV-457-2 against 28fca4179a347dac054a9d885bd4f1577623bef6.

## Authoritative inputs and status

Ticket via recon-DEV-457 §1: document and verify the Windows daily-driver setup; run API on host with durable container-backed services, document Ollama setup, supply a Playback:Players example, and verify BS Player opens an imported movie. Depends on DEV-458 and DEV-459. Phase A spec and owner Gate 1 precede implementation.

Facts come exclusively from recon-DEV-457 and recon-DEV-457-2. Full question/answer exchanges, cited bases, rationale and implications are preserved in CONCLUSIONS.md Q1–Q8. Those explicit decisions govern this brief. The original Ollama recon request is satisfied by supplement §12; it is no longer a blocker.

## Every grill answer

1. Q1: Add root docker-compose.daily-driver.yml for durable Postgres and RabbitMQ only, with named volumes and loopback-published ports. Windows host runs API, Ollama, and Vite. Keep full Compose and disposable AppHost untouched. Patron authorises configuration entrypoint under charter §2.3 item 2. Basis: ticket, constitution 709–710, recon §§2–3.
2. Q2: Use dedicated non-default Compose project name, operator-configurable ports and local credentials. Commit no real credentials or connection strings. Verify imported movie and API readiness survive service restart/recreate retaining volumes, and separately verify retained RabbitMQ broker state. Document collisions, one entrypoint at a time, non-destructive stop/down without -v, and destructive volume removal separately. Patron rules on secrets under item 5. Basis: ticket durability, constitution 444, recon §§2–3,5.
3. Q3: Document Windows host Ollama installation, listener, all-minilm, startup ordering, actual embedding validation and diagnostics. Correct README claim that Ollama never blocks API boot; /health/ready is not the Ollama probe. Patron authorises README correction under item 6. No runtime behavior changes. Keep current image pins and fixture unchanged; existing image-pin drift: noted, no ticket. Basis: recon §4.
4. Q4: Put full runbook and illustrative Playback JSON in README.md only. Include explicit loopback-host LocalPlay opt-in and Playback:Players entries with Name, ExecutablePath placeholder, and operator-selected Formats. No real paths, credentials or enabled shipped defaults. Local values go to user-secrets or ignored local development settings. Compose LocalPlay remains off. Patron authorises manual exercise through existing gated endpoint, touching LocalPlay/secrets/Process.Start under item 5, with no new launcher/shell logic or code/API changes. Basis: recon §§5–6; constitution 444,446–449,538.
5. Q5: Real BS Player proof waits for DEV-458/DEV-459 delivered contracts and Phase B drift recon. Import local movie through existing real import path; explicitly enable loopback LocalPlay with locally configured BS Player; call POST /movies/{id}/play; record returned status, visibly observed BS Player opening the resolved imported file, and playback-history evidence. Write scrubbed runtime-acceptance.md with date, delivery SHA, dependency SHAs/receipts, Windows/player versions, selected format and config summary. Redact real library/player paths and secrets. Automated IProcessStarter substitutes cannot close this proof. Missing host/player/media/dependencies means Could not run with reason, runtime task open, no ticket acceptance or Done claim. Basis: recon §§5–8.
6. Q6: Final implementation files are README.md, new docker-compose.daily-driver.yml and specs/DEV-457/runtime-acceptance.md only. Phase A artifacts stay under specs/DEV-457. Reuse current Postgres/RabbitMQ images subject to Phase B recon confirmation. No packages, projects/layers, schema/API/status/DTO change, frontend code, AppHost/full-Compose edit, launcher rewrite or implementation of dependency tickets. Charter items 1/3/4 authorise none; 2/5/6 are explicitly ruled by Q1–Q4. Recommended Ollama installation uses official OllamaSetup.exe and background app, then pull/show/list model and real API startup; no winget, downloaded-script or new service-manager recommendation. Title/structure are taste choices in ASSUMPTIONS.md. Basis: recon §§1–10 and supplement §12.
7. Q7: After Gate 1 and delivered dependencies, Wisp checks exact-head drift, current image/data-mount/config/startup contracts; implement Compose and README; Gauge validates rendering, loopback ports, volumes, project isolation, startup readiness and retained application/broker state; execute separate real Windows/Ollama/import/player proof. Use existing harness gates at their stage with scope-empty SKIPPED never PASS. No tests mirroring YAML or invented unit/property tests for prose/config. Conductor records propertyTests opt-out for no new domain invariants. Applicable gate failures/Could not run block; runtime Could not run leaves acceptance open. Basis: recon §§3–8 and pipeline gate policy.
8. Q8: Shared understanding reached; grill closes at 8/12. Review closing bar, frozen scope and two-round cap below approved. No structural owner checkbox, new glossary concept or ADR required. Quill drafts spec/plan/tasks next, followed by read-only analysis, plan challenge, plan freeze, owner Gate 1. Basis: ticket, recon §§1–10 and supplement §12; no ticket contradiction or constitution departure.

## Approach and file ownership

The dedicated Compose entrypoint supplies infrastructure for a Windows host API, rather than changing the lifecycle of existing entrypoints. PostgreSQL and RabbitMQ data use named volumes, with no external interface bindings. Host-specific credentials, connection settings, library root and player executable stay operator-supplied and untracked.

README.md is the sole operator runbook and playback-example location. It must explain prerequisites, local configuration, service/API/frontend startup, verification, ordinary stop/restart, destructive reset warning and troubleshooting. Host API command uses the existing src/LamuFlix.Api project; host frontend uses the existing web application. Exact commands/config contracts must use Wisp recon evidence; newly missing facts return to recon, never inferred. No new script or tracked settings template is authorised.

Compose file: new docker-compose.daily-driver.yml only. Existing docker-compose.yml, .env.example, docker/api.settings.example.json, AppHost files, C# and web code remain outside this file set.

Acceptance record: specs/DEV-457/runtime-acceptance.md is produced during Phase B from observed results, never prefilled as passing. Phase A creates spec.md, plan.md, tasks.md and these grill records under specs/DEV-457 only.

## Host Ollama instructions grounded in supplement

Use official Windows installer [download](https://ollama.com/download/OllamaSetup.exe) with [official Windows documentation](https://docs.ollama.com/windows). Open the installed background app; default endpoint is http://localhost:11434, aligned with host API default. Follow [official CLI documentation](https://docs.ollama.com/cli): ollama pull all-minilm, ollama show all-minilm, ollama list (alias ls). These vendor facts are supplied by recon-DEV-457-2 §12.2–12.3.

Local model visibility and successful real host API boot are both required. The actual startup validator probes a 384-dimensional embedding before HTTP binding; readiness alone does not prove Ollama. Diagnose unreachable listener, absent model and startup validation failure distinctly. Do not substitute container exec steps for host commands or equate model listing with functional startup.

## Task ordering and acceptance coverage

1. Phase A: Quill drafts spec, plan, tasks from this brief and recon. Keel performs read-only cross-artifact analysis; challenge and adjudication precede plan freeze. Owner Gate 1 precedes implementation.
2. Phase B prerequisites: confirm owner approval, delivered DEV-458 and DEV-459 receipts, and Wisp exact-head drift recon for all authorised implementation files and service/config contracts. Dependency plans or provisional specs alone do not prove delivered behavior.
3. Add dedicated durable infrastructure and README instructions/example, using local config and explicit loopback opt-in. Do not implement dependency-ticket fixes here.
4. Gauge checks Compose rendering using locally supplied non-secret validation values, named-volume mount correctness, host-only bindings, project isolation and collisions, host API connectivity/readiness, and retained imported movie plus broker state through restart/recreate with volumes retained. Volume existence alone is insufficient proof; do not claim queue/message semantics from mounts alone.
5. Execute documented Windows install/model/startup/import/playback steps. Capture separate BS Player visible-file-open and playback-history proof with scrubbed runtime evidence. Record each criterion independently.
6. Run applicable harness checks at the Build workspace pipeline stage, review against exact diff, adjudicate and remediate within cap. Do not close ticket acceptance until required runtime proof succeeds.

## Verification and gate expectations

Automated/static validation and manual runtime acceptance are distinct evidence. No Phase A gate or acceptance PASS is claimed here. Pipeline scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS; disabled gates retain their exact SKIP status. N/A and Could not run remain distinct.

No domain invariant is introduced: Conductor must record propertyTests: opt-out — no new domain invariants in docs/config changes in task note; that receipt is not implied by this brief. Mutation runs only if eligible production C# changes, which require a fresh scope decision. No frontend gates are claimed passing for unchanged web code. Existing required gate failures and applicable Could not run block. Missing runtime prerequisites leave the manual task open and block ticket acceptance, never turn into automated proof.

## Loop discipline

Closing bar: Critical and High findings block closure. Medium/Low findings receive explicit Follow-up or noted dispositions. Unmet ticket acceptance or unsafe exposure blocks regardless of severity. Out-of-scope findings follow charter tracker rules; image drift is already noted, no ticket.

Frozen scope: Windows daily-driver host API with durable isolated loopback container Postgres/RabbitMQ; official host Ollama setup and corrected startup documentation; placeholder Playback:Players example; real imported-movie BS Player and history proof after DEV-458/DEV-459, within Q6 files; anything else is a follow-up issue, not a finding in this round.

Round cap: two review/fix rounds. Unresolved above-bar findings at cap mean blocked, never waiver. This is review-loop scope settlement, not a PLAN FROZEN, Gate 1 or merge-clearance claim.

## Next handoff

Quill owns spec.md, plan.md and tasks.md drafting. Keel owns decisions, read-only analysis and later plan adjudication. No new domain vocabulary or ADR is necessary. No remaining grill question, cap fallback or owner structural checkbox is open. The settled operational terms and conclusions are ready to feed speckit-specify; drafting must not contradict existing glossary or ADRs.
