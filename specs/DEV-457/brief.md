# DEV-457 — Phase A grill brief (partial)

Status: awaiting recon; not frozen; Gate 1 remains closed. Questions completed: 3 of at most 12. Worktree: `F:/Dev/LamuFlix.worktrees/feature-457-spec`, branch `feature/457-spec`. Recon base: `659a2e9b739c092dc8565ee31ed6bc5bcc4b68e3`.

## Authoritative scope

Ticket scope from `recon-DEV-457` §1: run the API on the host with durable container-backed services, document Ollama setup, supply a `Playback:Players` example, and verify BS Player opens an imported movie. Dependencies DEV-458 and DEV-459. Phase A spec and Gate 1 precede implementation.

## Confirmed decisions

Every full question, Patron answer, basis, and implication is recorded in `CONCLUSIONS.md` Q1–Q3.

1. Q1: Add root `docker-compose.daily-driver.yml` for durable Postgres and RabbitMQ only, using named volumes and loopback-published ports. Windows host runs API, Ollama, and Vite. Existing full Compose and disposable AppHost remain untouched. Patron authorises configuration entrypoint under charter §2.3 item 2.
2. Q2: Use a dedicated non-default Compose project name, configurable ports and locally supplied credentials. Commit no real credentials or connection strings. Prove persistence through restart/recreate with retained volumes: imported movie and API readiness survive; RabbitMQ retains broker state. Document collision handling, one entrypoint at a time, and routine stop/down without `-v` separately from destructive volume reset. Patron rules on secrets under item 5.
3. Q3: Document host Ollama installation, listener, `all-minilm`, startup ordering, actual embedding validation and diagnostics. Correct the existing README claim that Ollama never blocks API boot; `/health/ready` is not an Ollama probe. Patron authorises README correction under item 6. Leave image pins, fixture, existing Compose, and runtime startup behavior unchanged. Image drift: noted, no ticket.

## Plan decisions established so far

Approach: dedicated durable service topology and Windows host runbook; keep the other entrypoints separate.

Files authorised so far: new `docker-compose.daily-driver.yml`; existing `README.md`; these three Phase A grill records. Local machine configuration uses user-secrets or git-ignored local settings per constitution 444 (recon §5). Final example placement and any additional tracked files remain undecided.

Verification established so far: retained imported movie/API readiness and retained broker state after non-destructive restart/recreate; actual Ollama startup validation distinguished from readiness. No verification has run in Phase A.

Task ordering established so far: complete missing recon and grill before Quill drafts spec, plan and tasks; owner Gate 1 before implementation. Dependencies must be accounted for before the final real-player runtime proof; exact dependency acceptance and pickup checks remain to be settled.

## Open decisions and loop terms

Not yet settled: `Playback:Players` placeholder shape and placement, explicit LocalPlay/process-execution ruling, BS Player live proof and evidence format, dependency completion bar, remaining charter items, final file set, final test strategy and gate applicability, closing severity bar, frozen scope, and review round cap. No default or recommendation for these is a decision yet.

## Required recon before resuming

needs recon: What are the current official Windows Ollama installation instructions and download URL, including supported install/start commands and the model-pull/availability verification steps for all-minilm?

Resume at Q4 after Wisp supplements `recon-DEV-457`; do not repeat the confirmed Q1–Q3 decisions. No spec, plan, tasks, ADR, runtime claim, or plan-freeze claim is produced by this partial grill.
