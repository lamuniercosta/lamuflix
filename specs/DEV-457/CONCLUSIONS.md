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
