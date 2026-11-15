# DEV-471 — Patron Rulings (CONCLUSIONS)

## Q1: Review loop terms and frozen scope
- **Verdict**: Approved (`recon-DEV-471:4-37`; `DEV-471:3-29`; `task-pipeline:25-30`; `lamuflix-team-charter:16-31`; `specs/PRODUCT.md:18-48`).
- **Basis**:
  - **Standing review loop terms (cited, not re-ruled)**: Per standing pipeline rules (`task-pipeline:27-29`; `specs/DEV-501/CONCLUSIONS.md:3-8`), review is capped at 2 rounds (`<= 2` fix commits per round), all verified unresolved Critical, High, Medium, and Low contract/safety/acceptance findings block closure (escalating to owner review if any remain at the 2-round cap), and speculative abstractions defer (`specs/PRODUCT.md:18`).
  - **Frozen scope & ADR 0043**: Scope is frozen to `DEV-471` (`recon-DEV-471:4,8-25`; `DEV-471:9,21-29`): per-PR Compose startup plus real end-to-end smoke covering `start`, `import`, `enrich`, `thumbnail`, and `play` with isolated deterministic fixtures/stubs, CI wiring, cleanup, and failure evidence under claimed ADR `0043` (`DEV-471:26-29`; `recon-DEV-471:32-37`); anything else is a follow-up issue, not a finding in this round.
  - **Section 2.3 boundaries & recon flag**: Under `lamuflix-team-charter:18-31` and `specs/PRODUCT.md:41-48`, zero new dependencies, layers, schema changes, public API changes, or hardcoded secrets are authorized (`recon-DEV-471:9,26-29`); `recon-DEV-471:4,24,29` notes that exact YouTrack body wording beyond the intake summary and whether `play` means browser/HTTP playback versus `Features:LocalPlay` are not established in current recon, so flag bounded recon only if a subsequent question requires ticket-body detail beyond `web/e2e/compose.e2e.yml` + existing API/browser smoke flows.

---

## Q1 full decision exchange (Keel record)

**Question (Keel):** /grill-with-docs DEV-471. Phase 2 grill, at most 12 questions, one decision per exchange. Worktree F:/Dev/LamuFlix.worktrees/feature-471-spec branch feature/471-spec HEAD e0658703; claimed ADR 0043. Facts only from recon-DEV-471; ticket intake there says require a Compose smoke test on every PR covering start, import, enrich, thumbnail, play. Q1: What review loop terms do you rule? I recommend all unresolved findings including Low block closure, two review rounds, and frozen scope limited to per-PR Compose startup plus the real import/enrich/thumbnail/play smoke, isolated deterministic fixtures, CI wiring, cleanup and failure evidence; anything else is a follow-up issue, not a finding in this round. This enforces the ticket while bounding review churn; cost is deliberate adjudication of every finding and escalation at the cap. Please give a cited ruling under section 2.3, flag any required recon rather than inventing facts, and reply via maestri ask Keel with your full answer.

**Answer (Patron):** DEV-471 Q1: Approved; full cited ruling in C:/Users/lamun/AppData/Local/Temp/patron-dev-471-q1.md and committed at specs/DEV-471/CONCLUSIONS.md.

The complete cited ruling, rationale and implications are preserved in the preceding Patron-authored Q1 entry. Confirmed terms: two rounds, at most two fix commits per round; verified unresolved Critical, High, Medium and Low contract/safety/acceptance findings block closure; unresolved findings at the cap escalate for owner review. Speculative abstractions defer. Gate statuses retain their standing meanings; scope-empty SKIPPED remains non-blocking and never PASS.

**Recon boundary before Q2:** recon-DEV-471:4,24,29 does not establish the full ticket Scope & Technical Design/acceptance criteria or the intended meaning of play. Request these facts before choosing the playback acceptance oracle. No decision on browser playback, HTTP media delivery, or local process execution has been made.

---

## Q2: Authoritative ticket scope, `/hubs/` live updates, DEV-440 regression proof, and `play` definition
- **Verdict**: Approved — NOT `blocked: structural` (`recon-DEV-471:39-64`; `lamuflix-team-charter:16-31`; `specs/PRODUCT.md:23-24,32-48`).
- **Basis**:
  - **Ticket mandates (Q1 frozen-scope correction)**: Per `specs/PRODUCT.md:23-24` and `lamuflix-team-charter:28`, the authoritative `DEV-471` ticket description (`recon-DEV-471:43-44`) mandates a required PR-to-`main` check using shipped `docker-compose.yml` (`recon-DEV-471:59-60`, with the `web/e2e/compose.e2e.yml` stub/env overlay at `recon-DEV-471:61-62`) covering healthy stack startup, fixture import, stubbed-provider enrichment, thumbnail generation, library listing, `/hubs/` live updates, `play`, and proof that reverting `DEV-440` fails; Q1 frozen scope is explicitly amended to include library listing, `/hubs/` live updates, and `DEV-440` regression failure proof.
  - **Patron interpretation of `play` vs `Features:LocalPlay` (§2.3 item 5)**: The ticket names `play` (`recon-DEV-471:43`) without specifying local process launch vs browser playback (`recon-DEV-471:44`); because shipped `docker/nginx.conf:13-15` blocks `POST /api/movies/<id>/play` with `403`, `docker/api.settings.example.json:3-5` sets `Features:LocalPlay` `false`, and `docker/nginx.conf:17-36` proxies `/api/` and `/hubs/` (`recon-DEV-471:48-56`), Patron rules under `lamuflix-team-charter:23` and `specs/PRODUCT.md:34,47` that `play` in the Compose smoke test is defined as successful browser in-page media playback through shipped nginx (`web/e2e/dev455-playback.e2e.ts:39-42,101-111`; `web/playwright.config.ts:3-19`), keeping `Features:LocalPlay` `false` and preserving the nginx `403` block on `POST /api/movies/<id>/play`.
  - **No structural escalation (`lamuflix-team-charter:25-27`)**: Neither escalation condition applies because all ticket deliverables (`recon-DEV-471:43-44`) are preserved without contradiction and no constitution departure is required (`specs/PRODUCT.md:36-39`).


---

## Q2 full decision exchange (Keel record)

**Question (Keel):** [from Keel] /grill-with-docs DEV-471 resume Q2 of at most 12. Follow-up recon-DEV-471:39-64 is now authoritative: ticket requires a PR-to-main check using shipped docker-compose.yml, healthy stack, fixture import, stubbed-provider enrichment, thumbnail, library listing, /hubs/ live updates and proof reverting DEV-440 fails. Existing web/e2e/compose.e2e.yml is an overlay on shipped base; nginx blocks process-launch play with 403 while /hubs/ is proxied. Q2: Do you approve correcting Q1 frozen scope to explicitly include library listing, /hubs/ live updates and DEV-440 regression proof, and define play as successful browser in-page playback through shipped nginx rather than invoking the blocked process-launch route? I recommend this because it preserves all ticket deliverables and deployment boundaries; cost is a real Chromium media assertion and live-update oracle rather than endpoint-only checks. Keep LocalPlay false and preserve nginx 403. Please cite your ruling, distinguish ticket mandates from interpretation, and identify any structural escalation. Record your cited ruling append-only in CONCLUSIONS.md, then report back with maestri ask Keel; Keel will append this full question before Q3.

**Answer (Patron):** DEV-471 Q2: Approved (NOT blocked: structural); full cited ruling in C:/Users/lamun/AppData/Local/Temp/patron-dev-471-q2.md and committed at specs/DEV-471/CONCLUSIONS.md.

**Normalized ruling and implications:** The ticket mandates a required PR-to-main check against shipped docker-compose.yml covering healthy startup, fixture import, stubbed-provider enrichment, thumbnail generation, library listing, /hubs/ live updates, play and proof reverting DEV-440 fails (recon-DEV-471:43-44). Q1 frozen scope is amended accordingly. Patron defines play as successful browser in-page media playback through shipped nginx, with Features:LocalPlay false and the nginx 403 process-launch block preserved (recon-DEV-471:48-56,59-63; charter section 2.3 item 5). This preserves ticket deliverables and requires no constitution departure; no structural escalation. The preceding Patron Q2 entry contains formatting corruption; this normalized record preserves its meaning without rewriting that append-only entry.

**Recon required before Q3:** Identify DEV-440 change, relevant commit(s), and expected observable failure when it is reverted. recon-DEV-471:43 names this proof requirement but does not establish what DEV-440 changed or which failure constitutes the proof. No regression mechanism is decided.


---

## Q3: Two-phase smoke architecture, disposable DEV-440 regression proof, and ADR 0043 tradeoff
- **Verdict**: Approved (`recon-DEV-471:10-25`; `lamuflix-team-charter:16-28`; `specs/PRODUCT.md:23-24,32-48`).
- **Basis**:
  - **Two-phase architecture within one required PR check**: Because `web/e2e/compose.e2e.yml` (`010e0cb6`) overrides `api.environment.Ollama__Endpoint` to `http://127.0.0.1:${E2E_STUB_PORT}/` and would mask a revert of shipped `docker-compose.yml` commit `4646476f` (`Ollama__Endpoint: http://ollama:11434/`, `recon-DEV-471:11-18`), the required check must run two isolated phases: (1) plain shipped `docker-compose.yml` startup without the e2e endpoint override (including real Ollama readiness/model provisioning) proving the API reaches healthy (`recon-DEV-471:15,20,23`), followed by (2) a separately isolated `docker-compose.yml` + `web/e2e/compose.e2e.yml` stack for the deterministic stubbed full flow (`import`, `enrich`, `thumbnail`, library listing, `/hubs/` live updates, and browser `play`, `recon-DEV-471:17,24`).
  - **Non-mutating DEV-440 regression demonstration**: The required regression proof must execute the Phase 1 plain-stack health assertion against a disposable controlled revert of `4646476f` without mutating the PR worktree/checkout, verifying that API startup fails to reach healthy (`recon-DEV-471:20,23`), while the positive candidate run passes both phases.
  - **ADR 0043 record**: Record the two-phase boundary, isolation between phases, and the explicit runtime cost tradeoff (real Ollama provisioning plus a second stack startup as the necessary price of unmasked `4646476f` coverage alongside deterministic stubbed full-flow coverage) in ADR `0043` (`lamuflix-team-charter:16-28`; `specs/PRODUCT.md:34`).
