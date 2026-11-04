# DEV-445 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-04
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-445-spec` · branch `feature/445-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask (plus requirements checklist + this receipt). Grill already closed (`brief.md` Q1–Q5); mandatory `before_specify` grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge). `.specify/feature.json` already pointed at `specs/DEV-445` and was left unchanged.

**Inputs**: frozen `specs/DEV-445/brief.md` (Q1–Q5), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, Maestri notes `DEV-445`, `recon-DEV-445`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only).

**Artifacts written (Quill-owned)**:

- `specs/DEV-445/spec.md`
- `specs/DEV-445/plan.md`
- `specs/DEV-445/tasks.md`
- `specs/DEV-445/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR bodies (none for this ticket), constitution, production/test code, `harness.yml`, `design.md` (Palette-owned).

**Owner checkbox**: none (brief Q5 — zero owner escalations). No constitution amendment, ADR, or glossary/`CONTEXT.md` rewrite authorized. Taste assumptions already present in `ASSUMPTIONS.md`.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS and recon facts.

**needs decision**: none. Brief Q1–Q5 freeze merge/IMDb identity, Compose defaults and secret docs, healthy-empty facet behavior, test/file allowlists, runtime Bond acceptance, gates/ordering, and loop discipline with no open gaps for drafting.

**Encoding notes (not new decisions)**:

- No `research.md` / `data-model.md` / `contracts/` — public contract chain untouched; decisions frozen in brief (precedent DEV-426/DEV-439/DEV-447).
- SMOKE-001 encodes brief Q4 live Compose Bond acceptance; SMOKE-002 encodes healthy-empty facet hide; web unit TDD is separate evidence (ui: tag; web-implement).
- Palette `design.md` called out as required before plan challenge; Quill does not author it.
- Retain DEV-358 merge ownership; production chain change is minimal IMDb forwarding only (Q1).

---

## Repair round 1 (2026-11-04) — plan-challenge Q6

**Method**: Apply Keel's single numbered fix list from `DEV-445-quill-repair-request.txt` after Q6 append/read-back in `brief.md` and `CONCLUSIONS.md`. Exclusive ownership: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. Did not edit brief/CONCLUSIONS/ASSUMPTIONS/design/production/`harness.yml`. Did not declare PLAN FROZEN or implementation clearance. Status set to repaired for Keel freeze verification; Gate 1 remains closed.

**Additional inputs**: Q6 ruling; `recon-DEV-445-2`; adjudication-DEV-445 dispositions.

**needs decision**: none.

**Item dispositions (applied)**:

1. Sentry F1 — aligned US1/Edge Cases/plan Constitution·Strategy·Risks/T004/T006 to Q6 blank/whitespace-key semantics (not disabled equivalence; zero TMDb HTTP; miss→ProviderUnavailable retries; hit→Found/Unavailable preserving membership); README opt-out warning; no registration edits; no out-of-allowlist e2e retry test.
2. Sentry F2 — documented RateLimited/QuotaExhausted/ProviderUnavailable collapse; T020 expected/imported/matched counts + sanitized quota evidence; incomplete membership never passes; environmental Could not run only.
3. Sentry F3 / Ledger S-2 — T002 negative lock on tracked `api.settings.example.json`; ObservabilityComposeTests line-based + Directory.Packages.props root walk; README TMDb `.env` vs OMDb JSON.
4. Ledger S-1 — T012/T015/T016/Gates: C# tools on changed C# only; FilterPanel via web gates.
5. Ledger S-3 — T009 role/name absence + positive controls; replace obsolete empty-legend; toolbar close-panel-first.
6. Ledger S-4/S-6 — T003 four identity branches + StubMetadataProvider; T005 no null-id lookup rebuild.
7. Ledger S-5/S-10 — header/T003/Dependencies/examples: backend TDD optional; tests before T005; web T009→T010→T011 strict.
8. Ledger S-7 — T020 single operational sequence; referenced from US1 and plan Runtime/Risks.
9. Ledger S-9 — plan cites recon-DEV-445-2:30-42 ImdbId normalization; Q1 coalescing retained; FR-013/SC-007 retained (S-13 deferred).
10. Ledger S-11/S-12 — T008/T011/T013/T017/T018 verification receipts; design exists status; repaired for freeze verification; Gate 1 closed; no PLAN FROZEN by Quill.
11. T010 guidance — derived const beside `collectionFailed`; no useState/useEffect; FilterPanel.tsx only.

**Q6 collection wording reconciled**: Unavailable leaves new membership unset and preserves any existing membership per Q4 — never clears it (spec Scope #5, US2 #6, Edge Cases, FR-008, SC-004, T007, plan Persistence/Strategy).
