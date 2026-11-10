# DEV-454 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-09
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-454-spec` · branch `feature/454-spec`
**Method**: Spec Kit `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` from the closed grill; analyze-round-1 Quill repairs; plan-challenge repair round 1; then plan-challenge independent-reread repair round 2 (final allowed) from Keel's ONE numbered list. `$env:PYTHONUTF8=1` set for the drafting shell. Quill does not invent freeze.

**Inputs**: grill-closed `specs/DEV-454/brief.md` (Q1–Q8 + analyze-round-1 C1/C2 + Phase 2 step 6 + Ledger S4 final Patron disposition), `CONCLUSIONS.md` matching sections, `ASSUMPTIONS.md`, Maestri notes `DEV-454`, `recon-DEV-454` (supplement:7–22), `adjudication-DEV-454-Keel`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (cite / propose only).

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-454/spec.md`
- `specs/DEV-454/plan.md`
- `specs/DEV-454/tasks.md`
- `specs/DEV-454/checklists/requirements.md`
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, constitution (proposed text only in artifacts), ADRs (accuracy update planned for Build), production/test code, `harness.yml`, generated OpenAPI/TS, `CONTEXT.md` (append is a Phase B Build task T025).

## Owner checkbox (unticked — Gate 1 closed; PLAN FROZEN withheld)

Copied verbatim from brief Ledger S4 final Patron disposition:

- [ ] Approve the DEV-454 narrow constitution v1.13.0 amendment to Principles II and V: recognize existing LibraryFileAccessDeniedException as an expected failure and map it to sanitized 403 Forbidden in the single GlobalExceptionHandler, preserving all other exception mappings and the requirement that endpoints do not map exceptions independently.

### Proposed constitution amendment v1.13.0 (artifacts only — not applied)

**Principle II — exact expected-failure sentence addition:**

> Expected domain failures also include `LibraryFileAccessDeniedException` for library file policy and containment denial.

**Principle V — exact table-row addition:**

| Exception | Status |
|---|---|
| `LibraryFileAccessDeniedException` | 403 |

Reject route-local mapping. Single GlobalExceptionHandler arm only after **explicit owner approval** of the exact amendment text (or consistently incorporated owner-amended text) **plus** verified merge. T000 requires recorded **approval** (not any disposition) + verified merge; reject/unanswered/non-approval keeps pickup blocked even if a disposition and merge receipt both exist — do not count any answer as approval. T001 pickup verifies approved constitutional text.

Ordinary owner Gate 1 remains closed while the checkbox lacks explicit approval. Quill does not invent PLAN FROZEN or close the checkbox.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS, recon supplement facts, and adjudication dispositions. Additive internal filenames marked *(proposed)* pending Phase B seam verification (Q6). DEV-455/456 seams, Windows-1252 capability, and safe-open platform capability stated as Phase B STOP checks, not present-day delivery claims.

## Analyze round 1 repairs applied (prior; Keel numbered list)

1. **C1 (CRITICAL)**: Thin Api transport + sealed subtitle queries/handlers via AddHandler; no stream exemption.
2. **C2 (CRITICAL)**: Bounded CONTEXT.md four-term append via T025.
3. **U1 (MEDIUM)**: ProblemDetails via single IExceptionHandler; frozen statuses.
4. **I1 (MEDIUM)**: Honest mutation exits.
5. **U2 (MEDIUM)**: Token stability + other-track content under one-bad-track isolation.

## Plan-challenge repair round 1 applied (Keel numbered list + Ledger S4 resolution)

1. **Ledger S1 / Sentry F3**: Replaced reuse-only enumeration fiction with authorized existing `ILibraryFileReader`/`LibraryFileReader` same-directory regular-file enumeration extension + real-FS/adversarial tests; bounded existing ADR 0037 accuracy update; no new ADR/port/layer/package. Adapter owns safe open; check/open race guarantees explicitly unproven; T001 platform capability STOP then implementation/adversarial tests.
2. **Sentry F1**: Existing `LibraryOptions` four Range-validated positive bounds per brief; bounded processing and 422; exactly-at/over and invalid-config tests; precise options/reader/ADR files added to proposed implementation scope.
3. **Sentry F2 / Ledger S2**: Movie-id NUL exact-filename SHA-256 base64url; fresh allowlist match before open; collision fail closed; tampered/other-movie/vanished tests; no secret or token-to-path decode.
4. **Compass F1 / Ledger S6**: T007 owns reauthorization/enumeration/token lookup/cancellation/disposal/decoding; T008 pure transport. Core sealed immutable results map to Api DTO; no escaped live streams / Core→Api. Explicit shared-registration serialization; T025 execution note; BCL-only references.
5. **Sentry F4/F6/F7 / Compass F3**: Text-only labels + hostile basename; served ordering assertion; generation guard for current movie/selection incl stale list/content success/errors; Off/movie-change/unmount cleanup; list error distinct from empty success with Off/Subtitles unavailable/Retry and video continuity.
6. **Sentry F5**: Literal cue grammar boundary fixtures; preserve valid words/overlap; sanitized 422 only for actually invalid/unrepresentable structure; no blanket prefixing of NOTE/STYLE/REGION.
7. **Sentry F8 / Ledger S3/S5 / Compass F2**: Private no-store errors incl 500 and nosniff where applicable; MovieId and sanitized categories via TelemetryConstants; no new spans/metrics; encoding registration conditional on T001 capability proof with no mutable static init; verified registration/exception paths cite recon supplement.
8. **Ledger S4**: Still **blocked: structural** under charter 2.3(b). Proposed exact constitution v1.13.0 II sentence addition + V 403 row recorded in spec/plan/tasks/receipt; constitution itself not modified. Exact unticked owner checkbox carried. Single GlobalExceptionHandler mapping only after explicit owner amendment approval; route-local mapping rejected. T000 requires checkbox disposition + verified merge; pickup verifies approved text. Compliance NOT marked PASS. Gate 1 closed; PLAN FROZEN withheld. 403/404/422/500 contract preserved.

## Plan-challenge repair round 2 applied (final allowed; Keel numbered list)

1. **Normative `traceId` restored**: Spec FR-008 / API / error acceptance, plan V / test strategy, and T002/T006 assert every error response MUST include `traceId` (constitution V). Recon that the handler does not explicitly set `traceId` qualifies implementation facts only and does not remove the requirement. Assert through the configured ProblemDetails writer; T001 may add a bounded necessary writer-configuration task only if pickup proves missing — never claim already proven.
2. **T000 / FR-020 / checkpoints tightened**: Only **explicit owner approval** of exact amendment text (or owner-amended text incorporated consistently) **plus** verified merge unblocks pickup. Reject or unanswered checkbox remains blocked even if a disposition and merge receipt both exist. Do not count any answer as approval.
3. **Explicit generic sanitized 500 assertions**: T002 and T006 require generic sanitized 500 `ProblemDetails` cases on **both** subtitle routes asserting `private, no-store`, `traceId`, nosniff where applicable, and no path/cue leak. Existing 403/404/422 coverage alone does not prove all-errors headers.

**Checklist / receipt posture**: `checklists/requirements.md` deliberately unchecked pending Keel's independent reread. This receipt does **not** claim findings resolved, PLAN FROZEN, Gate 1, clean analyze PASS, or constitution amendment approval. No production edits; no constitution edits; Ledger S4 owner blocker stays. This is the final allowed repair round; remaining blockers after independent reread go to Bernstein — never auto-clear.

**needs decision / needs recon (current)**:

- **needs decision (owner / charter 2.3(b))**: Ledger S4 — **explicit approval** of the proposed constitution v1.13.0 II/V text (or owner-amended text incorporated consistently) via the unticked checkbox above. Reject/unanswered/non-approval keeps blocked. No other Quill `needs decision` remains from the numbered repair lists. Encoding stays T001-conditional. NOTE words are not blanket rejected.

---

## Durable Gate 1 prerequisites (not freeze)

Owner Gate 1 remains closed pending **explicit owner approval** of exact amendment text (or consistently incorporated owner-amended text) plus verified merge. Durable handoff: Patron provisional Gate 1 → Rigger opens the spec PR carrying checkbox + proposed II/V text → owner explicitly **approves** → user merges Gate 1 → Keel T000 records verified merge **and** that the checkbox was **approved** (not merely answered) in `CONCLUSIONS.md`. Reject/unanswered/non-approval remains blocked even if a disposition and merge receipt both exist. Build also waits for DEV-455 and DEV-456 delivery before consuming viewer/safe-reader seams (Q5; Q8). Runtime receipts remain required; Could not run ≠ PASS. Quill does not invent PLAN FROZEN, Gate 1 clearance, dependency delivery, constitution approval, or runtime acceptance.

---

## Encoding notes (not new decisions)

- No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief Q1–Q8 / Phase 2 step 6; public routes/DTO live in spec + generation tasks.
- SMOKE-001–004 distinguish planned criteria from observed Compose runtime acceptance; Could not run ≠ PASS.
- Local `tasks.md` Phase N IDs are for `/implement` / `web-implement` Phase N dispatch and are not canonical task-pipeline phases.
- T025 keeps historical numbering; executes after T009 / before T010 (Ledger S6).
- Does **not** claim Gate 1, runtime acceptance, dependency delivery, PLAN FROZEN, or constitution compliance PASS.
- Next: Keel independent reread of repaired drafts **with structural blocker**; Quill reports ready-for-reread-with-blocker, not clean/frozen. Remaining blockers after this final round go to Bernstein.

---

## Keel final independent reread verdict (2026-11-09)

Keel confirmed: both numbered repair rounds verified (including normative `traceId`, explicit-approval-only T000/FR-020, and generic sanitized 500 headers on both routes). Final read-only analyze: 26 requirement keys mapped to 26 tasks; no additional unresolved drafting issue; one Critical constitution issue remains Ledger S4. All 17 challenge findings have explicit dispositions in `adjudication-DEV-454-Keel`. **PLAN FROZEN withheld; Gate 1 closed.** Carry exact unticked owner amendment checkbox to the spec PR. No third repair round. This verdict does **not** authorize freeze or implementation. Quill final repair ask is complete; remaining Ledger S4 owner blocker goes to Bernstein.
