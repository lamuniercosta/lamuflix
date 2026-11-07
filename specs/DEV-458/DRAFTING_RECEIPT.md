# DEV-458 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-07
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-458-spec` · branch `feature/458-spec`
**Method**: `/speckit-specify`, `/speckit-plan`, `/speckit-tasks` from grill-closed brief; analysis round 1 fix list applied earlier; plan-challenge adjudication round-1 numbered repair list from Keel applied; same-round Q8 then Q9 remaining-blocker repair (not an extra challenger round).

**Inputs**: grill-closed `specs/DEV-458/brief.md` (Q1–Q9), `CONCLUSIONS.md` (including Q7–Q9), `ASSUMPTIONS.md`, Proposed `docs/adr/0036-local-launcher-trust-boundary.md` (Keel-aligned; Quill did not edit), Maestri notes `DEV-458`, `recon-DEV-458`, `recon-DEV-458-2` section 7, `recon-DEV-458-2-2` section 8, `recon-DEV-458-2-3` section 9, `adjudication-DEV-458-Keel`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only).

**Artifacts written/edited (Quill-owned) this round**:

- `specs/DEV-458/spec.md`
- `specs/DEV-458/plan.md`
- `specs/DEV-458/tasks.md`
- `specs/DEV-458/checklists/requirements.md`
- this receipt
- Maestri note `pr-body-DEV-458` (spec-PR body preparation only — does **not** claim the PR was published)

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0036 body (Keel owns ADR), constitution, production/test code, `harness.yml`, `CONTEXT.md`.

**Owner checkbox**: OPEN — exact unchecked brief Q9 checkbox carried into `spec.md` and `pr-body-DEV-458`. Text:

- [ ] blocked: structural — DEV-458 Q2 requires proving every effective listener loopback before serving when Features:LocalPlay=true, including whether the Kestrel configuration section defines endpoints and with what Url values (that section overrides all URL-family configuration). On net10.0 no documented API reads those values, or proves the section empty, before server start without either (a) reading IConfiguration by Kestrel/Endpoints/Url key strings outside options binding, or (b) adding a new options record binding the Kestrel section — both departures from constitution VII (constitution.md:440-443). Do you approve such a VII departure for this validator (and if so, which of (a) or (b)), or should DEV-458 Q2 be rescoped so LocalPlay=true is not required to be provable against Kestrel-section endpoints?

**Decisions invented by Quill**: none. Behaviour, hosting mechanism, Q8 pins, Q9 owner escalation, named accessors, and payload reconciliation taken only from brief/CONCLUSIONS Q1–Q9 and supplied recon facts.

**Selected design (not freeze / not Phase B proof)**:

- Dedicated API `IHostedService` `src/LamuFlix.Api/LocalPlayListenerValidationHostedService.cs` registered from `Program.cs`; pure parser `LocalPlayListenerAddressParser.cs`; request helper `Endpoints/PlayRequestBoundary.cs`; no `ServiceDefaults` change (brief Q7; CONCLUSIONS Q7; Learn URLs via `recon-DEV-458-2`).
- Q8 pinned mechanisms retained: nginx play deny → proxy 403; Aspire retain disabled web endpoint proxy; five headers only; named positive-control host; no throttle/containment/`--`/UseShellExecute expansion.
- Q9: I1 UNAVAILABLE; I0/I2/I3/I4/I6 named available; I7 Phase B T005 timing/parity; reject silent fail-all; preserve parameterless NotFound + titles `Forbidden`/`Not Found` with absent detail; pin new leak assertions in approved test zones; no handler/Core edit.
- This selects the design only. Phase B actual-startup and shipped-path evidence, Keel independent reread, **PLAN NOT FROZEN**, and Gate 1 (closed until owner answers) remain separately pending — none are claimed by this receipt. Spec-PR publication is Conductor-owned; Quill prepared the body note only and does not claim posted.

---

## Round-1 repair dispositions (Keel numbered list)

| # | Item | Disposition |
|---|---|---|
| 1 | Ledger H1 — VII feasibility OPEN | Applied earlier; now superseded by Q9: OPEN awaiting owner (I1 UNAVAILABLE), not unspecified recon. |
| 2 | Ledger H2 — complete Q7 supplement path | Applied; Q9 opens exact owner checkbox under §2.3(b). |
| 3 | Ledger M1/M2 + Compass L2 — exact paths + real-Kestrel observables | Named helpers/tests retained; I7 timing/parity pinned on T005. |
| 4 | Sentry H2/L3 + Compass L3/L4 — empty/unpopulated distinctions, net10.0, hostname, precedence pointer | Retained; default branch noted uncertifiable for LocalPlay=true until owner answers I1. |
| 5 | Compass L1 — FR-017 → T032 | Coverage map retained: FR-017 includes T032. |
| 6 | Ledger L2/L3 — pipeline seats + pure parsing | T025–T032 labelled pipeline with seat ownership retained. |
| 7 | Ledger M5 — checked-in citations | Retained. |
| 8 | Q8 decision input | Retained except custom 403 detail / custom NotFound message superseded by Q9. |
| Q9 remaining-blocker | Owner checkbox + accessor known result + payload reconciliation | Applied this round to all five Quill drafts + `pr-body-DEV-458`. |

---

## needs decision / needs recon (current)

- **needs decision (owner / user via spec PR checkbox) — blocking**: exact unchecked Q9 checkbox above. Constitution Check OPEN; freeze blocked; Gate 1 closed; no validator implementation until answered. No waiver/new options/raw strings/reflection/code endpoint/silent fail-all while waiting.
- **needs publication (Bernstein / Conductor)**: actual GitHub spec PR must be opened carrying the checkbox and `pr-body-DEV-458` content. Quill prepared the body note only; **does not claim posted** without publication receipt.
- **needs recon**: none remaining for accessor inventory or 403/404 payload construction (resolved by `recon-DEV-458-2-2` §8 / `recon-DEV-458-2-3` §9 + Q9).
- **needs recon (non-blocking / no ticket now)**: Sentry M5 dash-leading path/terminator facts; Sentry L1 DEV-438 containment guarantee — only if pursued later.
- **I7 Phase B proof**: mandatory T005 real-host observation; not a second owner question.

---

## Durable Gate 1 prerequisites (not freeze)

Gate 1 stays closed while the Q9 owner checkbox is unanswered. **PLAN NOT FROZEN**. Selected Q7/Q8/Q9 design does not invent PLAN FROZEN. Constitution Check VII OPEN awaiting owner blocks clean freeze/Gate 1 claims. Payload feasibility is resolved; structural owner decision remains open.

---

## Encoding notes (not new decisions)

- No `research.md` / `data-model.md` / `contracts/` — decisions in brief Q1–Q9; public route/statuses unchanged.
- Nothing visible in SPA; Compose-stack UI smoke omitted; nginx/Aspire runtime smoke is Phase B verification (FR-007 / T022–T023), not a user-facing SMOKE-###.
- Local `tasks.md` phase numbers are for `/implement` dispatch and are not canonical task-pipeline phases.
- Does **not** claim Gate 1, runtime acceptance, Phase B startup proof PASS, plan-challenge closure, PLAN FROZEN, or PR publication.
- Requirement coverage retained: 17 FR + 5 buildable SC = 22/22 mapped across 33 tasks (T000–T032), with FR-017 including T032.
