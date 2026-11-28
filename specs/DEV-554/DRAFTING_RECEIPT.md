# DEV-554 drafting receipt (Quill)

**Date**: 2026-11-28
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-554-spec`
**Branch**: `feature/554-spec` (verified)
**PYTHONUTF8**: 1
**HEAD at repair** (no Quill commit): `508f8e48c98d12465d9f418a9c5e86b6b9a61e89` (Patron Step 3.3 ruling commit; draft artifacts remain uncommitted)

## Commands run

- `$env:PYTHONUTF8='1'` then `.specify/scripts/powershell/setup-plan.ps1 -Json` (resolved FEATURE_SPEC / IMPL_PLAN / PRIVATE_TICKET_DIR) — prior draft pass
- `/speckit-specify` / `/speckit-plan` / `/speckit-tasks` — prior draft pass at `305ea623`
- Analyze fix rounds 1–3 (Keel Phase 2 step 3.3): applied previously
- Phase 2 step 6 post-adjudication repair: applied exact 7-item Quill fix list from `CONCLUSIONS.md` to `spec.md`, `plan.md`, `tasks.md`, `contracts/subdl-v2-adapter.md`, `checklists/requirements.md`, this receipt; private IMPL_PLAN / TASKS mirrors synced byte-identical
- Phase 2 step 6 residual (Keel one-item): finished CONCLUSIONS.md fix 4 at the HTTP adapter boundary in T005/T007 + transport contract; private IMPL_PLAN / TASKS mirrors re-synced byte-identical
- Clarify: recorded as closed from grill in `spec.md` Clarifications (no interactive questions)
- Omitted `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q7 + Step 3.3 + Step 6 freeze inventable unknowns; no schema; adapter contract under `contracts/`
- Did not edit `CLAUDE.md` Spec Kit plan pointer (outside ticket file scope / care item 6)
- Did not edit `brief.md` / `CONCLUSIONS.md` (Keel/Patron owned); Step 3.3 + Step 6 Quill fix list consumed as authority

## Sources used (decisions vs facts)

- Decisions: `brief.md` Q1–Q7, Step 3.3 governance ruling (`brief.md:89–99`), Step 6 plan challenge refinements (`brief.md:102–109`); `CONCLUSIONS.md` Step 6 adjudication Quill fix list items 1–7
- Facts: Maestri notes `DEV-554`, `recon-DEV-554` (esp. 19–75 ticket text, 77–112 inventory, 83–98 seams, 85 health registration pattern, 108–110 SubDL docs, 111 DEV-551/549 absence at grill HEAD); `specs/PRODUCT.md` §§2–5; constitution.md:760-770, :782-807, :852-857, :944-948
- Quill owns: `spec.md`, `plan.md`, `tasks.md`, `contracts/subdl-v2-adapter.md`, `checklists/requirements.md`, this receipt
- Keel owns: `brief.md` / `CONCLUSIONS.md` (not edited). No `ASSUMPTIONS.md` present; Quill created none

## needs decision

_(none open — Step 6 Quill fix list items 1–7 applied; residual fix 4 HTTP adapter boundary closed in T005/T007 + transport contract; facts from recon-DEV-554. Out-of-envelope 409/TelemetryConstants edits remain gated on cited Patron rulings recorded in CONCLUSIONS.md at Build pickup, not inventable here.)_

## Analyze fix round 1 dispositions (Keel six-item list)

1. **CRITICAL (VII/VIII governance)** — Applied recorded Step 3.3 ruling (prior round).
2. **HIGH (year mapping unlock)** — Applied (prior round).
3. **HIGH (normal composition)** — Applied (prior round); further separated from 409 in round 2.
4. **HIGH (quota attribution)** — Applied (prior round); strengthened with pickup evidence in round 2.
5. **MEDIUM (T011 same-file)** — Applied (prior round); tightened to Phase 3 checkpoint in round 2.
6. **MEDIUM (ship checkpoint)** — Applied (prior round); reordered T017 before delivery PR / T016 in round 2.

## Analyze fix round 2 dispositions (Keel four-item list)

1. **T017 before delivery PR then T016 receipt** — Applied (prior round).
2. **T011 after Phase 3 checkpoint** — Applied (prior round).
3. **Explicit T001/T008 409 implementation and separate composition evidence** — Applied (prior round).
4. **Q2 attribution pickup evidence and Unknown fallback** — Applied (prior round).

## Analyze fix round 3 dispositions (Keel two-item list)

1. **CRITICAL Principle VI** — Applied (prior round).
2. **HIGH remove ship-ready alternatives** — Applied (prior round).

## Phase 2 step 6 Quill fix list dispositions (7 items)

1. **Principle VII (Ledger S-1)** — Applied: Gate 1 / FR-015 / plan VII / T000 now specify typed options entry, singular 409 clause **replaced** by provider-set/chosen-provider rules, checklist update (:782-784, :795-801, :946-948), OpenSubtitles binding/validation preserved, search aggregation change for blank OS + valid SubDL stated, DEV-501 / ADR 0041 in Sync Impact Report + version bump + unchecked checkbox + T000/rejection checks.
2. **Pickup, edit permissions and Principle VIII (Ledger S-2/S-3/S-5)** — Applied: plan VI/VIII + pickup/structure, T001/T008, contract Public API require exact landed paths/lines for 409/health/registration; T001 lists in-vs-out-of-Q4 409 files; Patron approval in CONCLUSIONS.md before T008 out-of-envelope edits; provider ID as runtime data under vendor-neutral Core names (including test/support); vendor-named Core constant needs constitution ruling; reuse of host/URI-redaction/logging/SRT seams with pickup verification.
3. **Guarded transport (Sentry F1/F2, Ledger S-4)** — Applied: FR-003/004, plan transport/verification, T001/T005/T007/T011, external contract — disable auto-redirect; per-hop HTTPS+allowlist validation; finite hops; key only on eligible initial origin request; credential-free redirected/separate ZIP fetches; validated single-segment `nId` + structural query encoding; T005 multi-hop/disallowed/exhaustion/invalid-id/metacharacter cases; T011 no credentials on all followed hops.
4. **Byte bounds, diagnostics and resource ownership (Sentry F3/F5/F6)** — Applied (reader + residual HTTP adapter boundary): FR-005/006, plan, reader/transport contract, T002/T003/T005/T007/T011 — actual-byte bound before ZIP open; header early-reject only; per-invocation limits; cancellation + disposal on all exits; fixed reasons / structured safe fields; omit untrusted strings from diagnostics; coverage for over-cap without length, concurrent calls, cancel/dispose, marker absence; existing SRT seam kept. **Residual (Keel)**: T002 already covers the reader; T007 + transport contract now state the bounded actual-byte read applies before any full response buffering or ZIP open, `Content-Length` only an early-rejection aid, and HTTP cancellation plus disposal of adapter-owned response/stream on rejection; T005 requires a controlled over-cap response stream without a reliable length proving the adapter enforces the bound without reading to EOF and disposes on rejection (existing test-only transport option if needed).
5. **Typed options and test transport (Sentry F4, Ledger S-3/S-4)** — Applied: plan, T001/T006/T007, contract — reuse landed typed options/HttpClient; optional provider out of startup validation; typed validator before outbound; typed section binding; T001 HTTPS WireMock or isolated test-only transport; T005/T006 non-HTTPS reject with zero outbound; landed structured logging + movie ID context.
6. **Quota parsing (Sentry F7, Ledger S-4)** — Applied: contract, plan, T001/T005/T007 — documented attribution + reset representation before mapping; malformed/negative/overflowing/incoherent → `Unknown` without throw; injected `TimeProvider`; header regression cases; search counters separate; zero `/me`.
7. **Test placement and gates (Ledger S-6/S-7)** — Applied: plan + tasks — standalone ZIP + FsCheck in `tests/LamuFlix.UnitTests/ZipSubtitleArchiveReaderTests.cs`; composition in T009; WireMock/health/composition in integration; removed T002/T003 `[P]` and parallel example; T003 before T002; T004 verification; no mutable static state; T014/T017 actual mutation verdict + correct tests on share/span failure, retain thresholds.

## Notes

- Prerequisite-resolved private mirrors: Spec Kit resolves IMPL_PLAN (and TASKS when present) from the private ticket directory. Paths: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-554\plan.md` and `...\tasks.md`. Canonical worktree copies under `specs/DEV-554/` remain authoritative for the Spec Kit PR; Quill keeps private mirrors byte-identical.
- Quill-named files within ticket-owned areas: `SubDlSubtitleProvider`, `SubDlSubtitleProvider.Log.cs` (if pickup verifies convention), `ZipSubtitleArchiveReader`, `ArchiveSubtitleProviderOptions`, `ArchiveSubtitleProviderOptionsValidator`, matching tests. Exact DEV-551 registration/409/health paths and DEV-549 fixture directory deferred to T001 pickup (`recon-DEV-554:85,111`).
- Gate 1 and runtime home-cluster acceptance not claimed. Freeze wording not asserted — plan/tasks remain Draft for Keel reread after this repair.
- Title-and-year year mapping remains a Q5 pickup stop/lock condition, not a drafting invention gap.
- Q2 attribution and reset representation remain pickup evidence conditions; Quill does not invent whether current docs attribute the header trio as download usage.
- Principle VI health/TelemetryConstants obligations and out-of-envelope 409 edits still require cited Patron rulings in CONCLUSIONS.md (Q4 / Step 3.3 / Step 6).
