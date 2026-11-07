# DEV-457 — Grill conclusions

Phase A only. Facts: canvas note `recon-DEV-457`, HEAD `659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3`. Decisions: Patron. Question cap: 12. These exchanges are append-only.

## Q1 — Daily-driver topology

**Keel:** I recommend a dedicated `docker-compose.daily-driver.yml` in the existing root, selecting only durable Postgres and RabbitMQ with named volumes and loopback-published ports; Windows host runs API, host Ollama, and host Vite, while existing full Compose and disposable AppHost stay separate. This meets ticket and constitution 709–710, avoids changing AppHost lifecycle, but adds one configuration entrypoint. Approve this approach and explicit charter §2.3 item 2 ruling for the new root file?

**Patron:** DEV-457 Q1 approved: dedicated docker-compose.daily-driver.yml at repo root is authorised under charter 2.3 item 2 as the new configuration entrypoint for DEV-457.
- Basis ticket scope: run API on host with durable container-backed services (recon-DEV-457 section 1).
- Basis constitution 709-710: Windows daily-driver host runs API as process while Postgres and RabbitMQ run in containers (recon section 2).
- Basis gap: existing compose publishes no Postgres/RabbitMQ ports and declares no durable volumes for them, and AppHost is explicitly disposable, so neither satisfies the ticket (recon section 3); keep both untouched, loopback-only ports and named volumes in the new file only.

**Implications:** New configuration entrypoint authorised; no new project, layer, package, or AppHost persistence conversion. Host frontend startup is documented alongside the host API.

---

## Q2 — Durability, isolation, and secrets

**Keel:** I recommend a dedicated non-default Compose project name and service data volumes for the daily-driver, explicit host loopback port mappings, and locally supplied credentials with no committed real values or connection strings. README must distinguish ordinary stop/restart and down without -v from destructive volume removal; verification must import a movie, restart/recreate service containers retaining named volumes, and confirm the movie and API readiness survive. RabbitMQ durability proof must cover retained broker state rather than claim volumes alone prove queue/message semantics. Keep exact machine ports and credentials operator-configurable, document collision handling and no simultaneous entrypoints. Approve durability and isolation acceptance plus charter 2.3 secrets ruling? Basis recon sections 2–3, ticket durable container-backed services and charter scrub rule.

**Patron:** DEV-457 Q2 approved: durability and isolation acceptance as proposed, with charter 2.3 item 5 secrets ruling that no real credentials or connection strings are committed and all machine values stay locally supplied and operator-configurable.
- Basis ticket and recon sections 2-3: durable container-backed services are required, yet existing compose declares no Postgres/RabbitMQ named volumes and publishes no such ports, and AppHost data is explicitly disposable.
- Basis local-values rule: recon section 5 cites constitution 444 for user-secrets or git-ignored local settings, and recon section 3 notes the production settings file is git-ignored with only a tracked template.
- Acceptance limit: restart/recreate retaining named volumes must prove imported movie and API readiness survive, RabbitMQ proof must show retained broker state not volume existence alone, and README must separate non-destructive stop/down without -v from destructive volume removal, port-collision handling, and no simultaneous entrypoints.

**Implications:** Durability is demonstrated by retained application and broker state. Operator-controlled values and evidence must be scrubbed; destructive reset is never part of routine startup.

---

## Q3 — Host Ollama documentation and existing README correction

**Keel:** I recommend Windows host Ollama documentation covering install from official Windows distribution, start or confirm listener at configured host endpoint, pull all-minilm, then start API only after model availability; document real startup embedding validation and failure diagnostics rather than treat /health/ready as an Ollama probe. Correct README claim that Ollama never blocks API boot to match recon section 4, without changing runtime behavior. Image-pin drift is separate: leave compose, fixture and existing pins unchanged, record noted, no ticket unless you judge broken behavior needs an existing/follow-up tracker record. Approve docs boundary and README correction under item 6? Official install commands and current download URL must be supplied by Wisp recon before drafting executable runbook steps, never invented.

**Patron:** DEV-457 Q3 approved: Windows host Ollama docs boundary and README correction as proposed, with charter 2.3 item 6 ruling authorising the README edit, and no runtime behaviour change.
- Basis recon section 4: host Ollama is documented only as an AppHost prerequisite at localhost:11434 with all-minilm, no Windows install or runbook exists, and the startup validator probes a 384-dimension vector and throws on failure while its health check is excluded from /health/ready, so /health/ready must not be presented as the Ollama probe and the README claim that Ollama never blocks API boot must be corrected.
- Drafting condition: official Windows install commands and download URL are not in recon, so executable runbook steps must wait for a Wisp recon supplement supplying them; do not invent them.
- Image-pin drift: leave compose, fixture and existing pins unchanged; record noted, no ticket, as recon section 4 shows drift without proven broken behaviour.

**Implications:** No image upgrades or runtime startup changes. Image-pin drift: noted, no ticket. Grill pauses for the missing official installation evidence before Q4; this is not a closed or frozen plan.


---

## Recon supplement and Q4 — Playback example and execution boundary

Official installation evidence is now supplied by recon-DEV-457-2 §12.2–12.3 against 28fca4179a347dac054a9d885bd4f1577623bef6. Q1–Q3 remain unchanged.

**Keel:** Put complete Windows daily-driver runbook and illustrative Playback JSON in README.md, not new runbook/template files. Playback example shows Features:LocalPlay true only for explicit loopback host configuration and Playback:Players array entries with Name, ExecutablePath placeholder for operator-discovered BS Player executable, and Formats as operator-selected extensions; actual values go only to user-secrets or ignored local development settings. Never ship enabled defaults, real paths or secrets, never add launcher/shell logic, and keep Compose LocalPlay off. Authorise explicit item 5 LocalPlay/secrets/Process.Start exercise via existing gated endpoint for the later manual proof; existing code/API remains unchanged. Basis recon sections 5-6 and constitution 444, 446-449, 538. Approve example placement and safety boundary?

**Patron:** DEV-457 Q4 approved: complete runbook and illustrative Playback JSON in README.md only, with charter 2.3 item 5 ruling authorising the later manual LocalPlay exercise through the existing gated endpoint and no code or API change.
- Basis recon sections 5-6: Playback Players entries are Name, ExecutablePath and Formats, LocalPlay defaults false with no shipped Playback example, and BS Player verification is a live-host Process.Start proof with no existing harness.
- Basis constitution 444, 446-449 and 538 as cited in recon section 5: player mapping is configuration never a database table, real values belong in user-secrets or ignored local settings, LocalPlay stays gated, and no shell launching is added.
- Boundary: example uses placeholders and operator-selected formats only, never enabled defaults, real paths or secrets, Compose LocalPlay stays off, and enabled use is explicit loopback host configuration only.

**Implications:** README owns the operator instructions and illustrative configuration. The real launch is later acceptance, not a Phase A claim.



---

## Q5 — Real BS Player acceptance and dependency sequencing

**Keel:** BS Player acceptance is a real Windows host exercise after DEV-458 and DEV-459 delivery is available and Phase B drift recon confirms their shipped contracts. Import a chosen local movie through the existing real import path, enable LocalPlay on loopback with locally configured BS Player, call existing POST /movies/{id}/play, and record returned status, observed BS Player opening the resolved imported movie file, plus playback-history evidence. Capture date, delivery SHA, dependency SHAs/receipts, Windows/player version, selected format and scrubbed config summary in specs/DEV-457/runtime-acceptance.md; redact real library/player paths and secrets. Existing automated tests cannot substitute for visible player launch. If host/player/media/dependencies are unavailable, report Could not run with reason, keep runtime task open, and do not claim ticket acceptance or Done. No new GUI automation or launcher changes. Approve proof and completion bar? Basis ticket and recon sections 5-8.

**Patron:** DEV-457 Q5 approved: BS Player proof and completion bar as proposed, recorded in specs/DEV-457/runtime-acceptance.md with scrubbed evidence only.
- Basis recon sections 5-6 and 8: the existing POST /movies/{id}/play handler resolves the primary video, launches it and records a PlaybackEvent, BS Player is absent from the root with no real-process harness, and automated tests substitute IProcessStarter so they cannot substitute for a visible launch.
- Basis recon section 7: DEV-458 hardening and DEV-459 path migration both change the flow this proof exercises, so the exercise waits for their delivered contracts and Phase B drift recon.
- Completion bar: unavailable host, player, media or dependencies means Could not run with reason, runtime task stays open, and no ticket acceptance or Done claim; passing requires returned status, observed BS Player opening the resolved imported file, and playback-history evidence with date, delivery and dependency SHAs or receipts, versions, format and scrubbed config summary.

**Implications:** Runtime acceptance remains a mandatory open task until observed; automated launcher seams alone do not close it.



---

## Q6 — File, charter, and editorial boundary

**Keel:** Final implementation file boundary: README.md, new docker-compose.daily-driver.yml, and specs/DEV-457/runtime-acceptance.md; Phase A records/spec-plan-tasks stay under specs/DEV-457. Reuse current Postgres/RabbitMQ image choices through Phase B recon, no package addition, new project/layer, schema change, API/status/DTO change, frontend code change, existing AppHost/full-compose edit, launcher rewrite or dependency-ticket implementation. Explicit charter items 1/3/4 ruling: none needed or authorised within this scope; items 2/5/6 governed by Q1-Q4. Use official installer download https://ollama.com/download/OllamaSetup.exe as recommended Windows path, open installed background app, pull/show all-minilm, list models, then prove real API startup; cite official docs from recon supplement. Do not recommend executing downloaded scripts or new service managers. README section title Windows daily-driver setup and ordered prerequisites/configure/start/verify/stop/troubleshoot structure are taste choices tagged [assumed]. Approve complete file/charter and editorial boundary? Basis recon sections 1-10 and supplement 12.

**Patron:** DEV-457 Q6 approved: complete file, charter and editorial boundary as proposed; log the README title and ordered structure as taste choices tagged [assumed] in ASSUMPTIONS.md.
- Basis recon section 9: charter items 1, 3 and 4 need none and authorise none in this scope, with items 2, 5 and 6 already governed by Q1 to Q4, and recon sections 1 to 10 support no package, project, layer, schema, API, frontend, AppHost, full-compose or launcher change.
- Basis recon supplement 12.2 to 12.3: the recommended Windows path is the official OllamaSetup.exe from the cited direct download URL, background app at localhost:11434, then pull and show all-minilm and list models before real API startup proof, with no winget line and no downloaded-script or new service-manager recommendation.
- Boundary: implementation files are README.md, new docker-compose.daily-driver.yml and specs/DEV-457/runtime-acceptance.md only, Phase A records stay under specs/DEV-457, and current Postgres and RabbitMQ image choices are reused subject to Phase B recon confirming them.

**Implications:** No structural owner escalation is introduced. README taste choices are recorded separately as assumed; no BS Player path or version is inferred.



---

## Q7 — Task order and verification plan

**Keel:** Proposed verification plan: after owner Gate 1 and delivered dependencies, Wisp first checks exact-head drift and current service image/data-mount/config/startup contracts; implementation then adds durable Compose config plus README; Gauge validates Compose rendering with locally supplied placeholders, loopback bindings, service mounts, explicit project isolation, startup readiness and restart/recreate retained application/broker state; execute documented Windows/Ollama/import/player steps and write scrubbed runtime receipt. Use existing harness gates at their pipeline stage with honest scope-empty SKIPPED statuses for unchanged C#/web, mutation only if eligible production C# changes (outside frozen scope requires decision), no unit/property tests invented for prose/config or tests mirroring YAML. Record propertyTests opt-out for absence of new domain invariants through Conductor task note. Applicable gate failures or Could not run block; runtime Could not run leaves acceptance open. Automated/static evidence and manual acceptance must be separately labelled. Approve task order and verification/gate expectations? Basis recon sections 3-8, charter and task-pipeline Phase 3 gate policy.

**Patron:** DEV-457 Q7 approved: task order and verification and gate expectations as proposed.
- Basis recon sections 3 to 8: no existing entrypoint proves host API with durable services, no test proves a real host boot Ollama requirement, and playback tests substitute IProcessStarter, so drift recon first, then Compose and README implementation, then Gauge rendering and restart proof, then the separate manual Windows and BS Player receipt.
- Gate rule: use existing harness gates only at their pipeline stage, report scope-empty exit 2 as SKIPPED never PASS, run mutation only for eligible production C sharp changes which are outside frozen scope and would need a fresh decision, and invent no unit or property tests for prose or config or tests mirroring YAML, with propertyTests opt-out for no new domain invariants recorded through the Conductor task note.
- Blocking rule: applicable gate failures or Could not run block, runtime Could not run leaves acceptance open, and automated or static evidence and manual acceptance stay separately labelled.

**Implications:** The implementation plan must give each acceptance proof a task and preserve honest gate labels. Conductor must record the property-test opt-out; this document is not that task-note receipt.



---

## Q8 — Shared understanding and loop terms

**Keel:** Close grill with Q1-Q7 as shared understanding, no owner structural checkbox currently needed and no new glossary concept or ADR required for this Size M operational runbook. Proposed review loop terms: Critical and High findings block closure; Medium and Low are explicitly dispositioned Follow-up or noted, never silently dismissed; any unmet ticket acceptance or unsafe exposure blocks regardless of severity. Frozen scope: Windows daily-driver host API with durable isolated loopback container Postgres/RabbitMQ, official host Ollama setup and correct startup docs, placeholder Playback:Players example, and real imported-movie BS Player/history proof after DEV-458/459, limited to Q6 files; anything else is a follow-up issue, not a finding in this round. Round cap two review/fix rounds; unresolved above-bar findings at cap mean blocked, never waiver. Current grill closes at 8 of max 12; Quill next drafts spec/plan/tasks from brief, then read-only analysis and plan challenge before plan freeze and owner Gate 1. Approve closing bar, frozen scope, round cap and shared understanding?

**Patron:** DEV-457 Q8 approved: grill closes at 8 of 12 on Q1 to Q7 as shared understanding, with closing bar, frozen scope and two-round cap as proposed, and no owner structural checkbox, new glossary concept or ADR required.
- Basis ticket and recon sections 1 to 10 plus supplement 12: every decided item implements the ticket scope and constitution 709-710 without contradicting ticket text or departing from the constitution, and all charter items 1 to 6 are either none or ruled in Q1 to Q6.
- Closing bar: Critical and High findings block closure, Medium and Low are explicitly dispositioned Follow-up or noted and never silently dismissed, and any unmet ticket acceptance or unsafe exposure blocks regardless of severity, with unresolved above-bar findings at the two-round cap meaning blocked and never waiver.
- Frozen scope: Q6 files only for the Windows daily-driver runbook, durable isolated loopback Postgres and RabbitMQ, official host Ollama and corrected startup docs, placeholder Playback example, and real BS Player and history proof after DEV-458 and DEV-459; anything else is a follow-up issue, and Quill next drafts spec, plan and tasks from brief before read-only analysis, plan challenge, plan freeze and owner Gate 1.

**Implications:** Grill closed at Q8, with no cap fallback. Quill may draft the Spec Kit artifacts; plan freeze, Gate 1, implementation, runtime acceptance, and merge clearance remain distinct later claims. No new vocabulary or ADR is required.

---

## Phase 2 step 6 — Plan-challenge adjudication (Q2/Q4/Q7 refinements)

**Patron:** DEV-457 step 6 approved: Keel may hand Quill the listed in-scope safety and evidence refinements to Q2/Q4/Q7 as repairs; no ticket contradiction or constitution departure, and Q6 file boundary and existing contracts are preserved.
- Basis Q2/Q7 and FR-002/FR-004/FR-018: rendered ports must assert explicit 127.0.0.1 bindings; Compose credentials are fail-closed references with no working committed defaults, on a separate untracked channel from API user-secrets/ignored settings; the dedicated project name is top-level and destructive reset is project-scoped; RabbitMQ proof uses a retained durable queue with a persistent test message observed before and after restart without consuming application queues; durability import uses an isolated scratch library.
- Basis Q3/Q4/Q7 and FR-005/FR-006/FR-008: production API startup validator stays enabled with host Ollama available before T006 Gauge proof; LocalPlay opt-in requires prior loopback confirmation; the runtime receipt gets an explicit scrub check for real paths, credentials, and connection strings.
- Basis Q6/FR-015 and Q8 closing bar: keep the current RabbitMQ tag, noting same-image durability does not certify image upgrades; assert no Dependabot coverage fact in Phase A and defer that fact to T001 Wisp recon; unsafe exposure or unmet acceptance still blocks regardless of severity.

---

## Phase 2 step 7 — gate1: provisional

**Patron:** DEV-457 gate1: provisional recorded; owner Gate 1 remains closed until the user merges the spec PR.
- Basis brief.md Phase 2 step 6 PLAN FROZEN: Keel independently read back Quill repair round 1 against all 20 adjudicated findings and R1–R9, with no unresolved above-bar finding.
- Basis checklists/requirements.md: User-Visible Outcome items are ticked, and spec.md states Nothing visible on the default Compose stack with a reason.
- Boundary: provisional status is not owner approval, implementation clearance, runtime acceptance, or merge clearance; T000 still records the verified user merge before implementation.
