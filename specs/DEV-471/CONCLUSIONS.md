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
