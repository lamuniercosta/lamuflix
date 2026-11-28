# DEV-551 drafting receipt (Quill)

**Date**: 2026-11-28
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-551-spec`
**Branch**: `feature/551-spec` (verified)
**HEAD at draft**: `6328e801` (plan-challenge fix round 2; uncommitted)
**PYTHONUTF8**: 1

## Commands run

- `/speckit-specify` — wrote `specs/DEV-551/spec.md`; quality checklist `checklists/requirements.md`
- `/speckit-clarify` — recorded closed from grill in `spec.md` Clarifications (no interactive questions; no critical ambiguities)
- `/speckit-plan` — wrote `specs/DEV-551/plan.md` (+ mirrored to private Spec Kit IMPL_PLAN); `contracts/search-response.md`
- `/speckit-tasks` — wrote `specs/DEV-551/tasks.md` (+ mirrored to private Spec Kit TASKS)
- `/speckit-checklist` — wrote `checklists/multi-provider-subtitles.md` (size L requirements-quality checklist)
- Supporting: this receipt
- Analyze fix round 1 (Keel numbered list 1–7): applied to `spec.md`, `plan.md`, `tasks.md`, `contracts/search-response.md`, `checklists/requirements.md`, `checklists/multi-provider-subtitles.md`, this receipt. No commit.
- Analyze fix round 2 (Keel numbered list 1–5): applied to the same artifact set. No commit.
- Analyze fix round 3 (Keel numbered list 1–4): applied to the same artifact set. No commit.
- Analyze fix round 4 (Keel numbered list 1–3): applied to the same artifact set after brief.md:69 / CONCLUSIONS.md:111 settlement ruling. No commit.
- Analyze fix round 5 (Keel numbered list item 1): applied to `tasks.md` (T010/T026), `contracts/search-response.md` proof list, `checklists/multi-provider-subtitles.md` (CHK027), `checklists/requirements.md`, this receipt. No commit.
- Plan-challenge fix round (Keel Quill fix list 1–6 at note DEV-551:47–53; Patron S5 at 6328e801): applied to the same artifact set. No commit.
- Plan-challenge fix round 2 (Keel Quill fix list 1–3 at note DEV-551:55–58; Ledger S1 / ADR 0052): applied to `spec.md`, `plan.md`, `tasks.md`, checklists, this receipt; private TASKS mirror refreshed to byte-identity. No commit.

## Sources used (decisions vs facts)

- Decisions: `brief.md` Q1–Q12 and Phase 2 plan-challenge decisions; `CONCLUSIONS.md` (incl. Patron S5 and plan-challenge dispositions); `ASSUMPTIONS.md` (Q6 `[assumed]`; S5); Proposed ADR 0052 for amendment framing (Keel-owned); round-4 item-2 settlement ruling at `brief.md:69` / `CONCLUSIONS.md:111` / note DEV-551:36
- Facts: Maestri notes `DEV-551`, `recon-DEV-551` (ports, callers, DTO **record** names — not enumerated legacy field/type/nullability tables); `specs/PRODUCT.md` §1–5; constitution cite-only in plan (including Enumerations serialized by `Name`); existing `SubtitleProviderOptions` / `SubtitleSearchResponse` / `SubtitleProblemReasons` shapes as runtime/OpenAPI authority for unchanged legacy contract
- Quill owns: `spec.md`, `plan.md`, `tasks.md`, `contracts/search-response.md`, `checklists/requirements.md`, `checklists/multi-provider-subtitles.md`, this receipt
- Keel owns: brief/conclusions/assumptions/ADR (not edited)

## Patron-approved identifiers (Ledger S5 — not draft attribution)

| Name | Role | Basis |
| --- | --- | --- |
| `Subtitles:ProviderOrder` | Explicit provider enumeration order | Q8 |
| per-adapter `Enabled` | Adapter-local enablement | Q8; Patron S5 |
| `MinimumRequestGap` | Per-provider call spacing (`>= TimeSpan.Zero`; no invented positive safety default) | Q8; Patron S5 |
| Outcome states `off` / `unusable` / `ok` / `quota` / `timeout` / `failed` | Closed state labels for Q2–Q5 outcomes; `failed` covers both `no_search_input` (no I/O) and `provider_unavailable` (after I/O) | Patron S5; Q2–Q5; contracts |
| Reason codes in `contracts/search-response.md` | Closed safe reason labels with exclusive pre-I/O precedence (disabled → `off`/`off`; else unsafe URL → `unusable`/`unsafe_base_url`; else missing/invalid credentials/timeout/gap → `off`/`provider_not_configured`) and request-time reasons (`ok`/`empty`/…/`no_search_input`); no optional empty slot | Patron S5; Q2/Q3/Q4/Q5/Q8 |
| ProblemDetails extension key `providers` | Settled **search** error-response extension matching ruled providers array shape; present on Q2 no-usable search 409 and legacy settled search errors; absent on request-level preempting settlement; **download** ProblemDetails unchanged without this key | Q3; M4; brief.md:69 |
| Core-side ordered provider lookup | Shared concrete lookup on Core side of existing `ISubtitleProvider` seam for search/download/Lookup/health; owns host-lifetime per-provider spacing and observation state | Q8; Constitution I; Ledger S4 (not a new port/layer) |
| Outside-Core typed-client composition | Named typed HTTP client registration in Infrastructure/Api | Q8; Constitution I |
| Enumeration (or equivalent) + JSON `Name` | C# closed `state`/`reason` types at Core subtitle outcome boundary; JSON Name conversion in approved API DTO scope; legacy reason types preserved | Constitution API and Contract Rules; Ledger S2; T001/T009 |
| Central ProblemDetails handler | Principle V single handler/`traceId`; T009 attaches settled-search data only through it; T014 keeps download mapping on it | Ledger S3; T001 |
| Disjoint HTTP status precedence | Request-level → existing (`providers` absent); no-usable → 409 (`providers` present); one-provider OpenSubtitles → legacy (incl. baseline non-200); multi-provider ≥1 usable at config-time freeze → 200 | Q2/Q3; brief.md:69; contracts |
| Search vs download 409 predicates | **Search**: all-enabled check → 409 only when no enabled provider usable; usable sibling leaves search configured. **Download**: after read-only, validate **bound** provider only → 409 even with usable sibling; zero paid calls to either provider; ProblemDetails without `providers` | Ledger S1; ADR 0052; FR-002 |
| Usability freeze | Provider usability fixed at request-time acquisition configuration validation; discriminating T010 fixture: multi-provider whose only config-usable provider settles timeout / search 429 / failure / no formable input with off/refused sibling → 200, empty candidates, ordered named outcomes (not Q2 409) | Q2/Q5; brief.md:69 |
| Dual HTTP opt-in | Credential-free adapter declaration **and** explicit typed opt-in (default disabled); four independent typed-client gates (permit both; refuse missing opt-in; refuse missing credential-free declaration despite opt-in; refuse credential/login despite both); OpenSubtitles HTTPS-only | Q4; H1; T006/CHK029 |
| Response-independent destinations | Adapter declarations or explicit typed config; refuse disallowed initial URL and redirect before any contact | Q4; M1 |
| Redacted unsafe-URL warning | Provider name + `unsafe_base_url` only | Q2/Q4; M2 |
| ValidateOnStart exception | Acquisition settings never abort host; invalid timing/missing credentials → off; unsafe URL → unusable; usable sibling remains | Constitution VII; M3/S1 |

## Analyze fix round 1 — dispositions

1. **Constitution I** — Core-side ordered provider lookup; typed HTTP client registration/composition outside Core; existing port retained (`plan.md`, `tasks.md` T004, `spec.md` FR-009).
2. **Task order** — OpenSubtitles adapt + green suite in foundation (T017–T019 in Phase 2); ranked binder T013 before search issuance/parity; T026 after T018 + T010.
3. **Constitution VII** — Request-time sole missing/invalid 409 on search and download with zero paid calls; read-only precedence on download; endpoint regression in T010/T014/T015; checklist CHK002/CHK040.
4. **Q5/Q7 test mapping** — FR-006 / T008 / T011 / T014 cover no formable input, missing IDs, stop-on-timeout/quota/failure, search 429 vs download exhaustion; T015 covers ranked issuance across providers/preferred languages at full capacity + expiry with injected time and deterministic randomness.
5. **contracts/search-response.md** — Deterministic `failed` mapping; unsafe-URL-scoped startup refusal vs credential-free dual opt-in; multi-usable 200 vs no-usable 409 vs single-OpenSubtitles legacy; ProblemDetails key `providers`; wire-level + generated-schema coverage; legacy fields referenced via unchanged contract (round 1 had claimed recon substantiation — corrected in round 2).
6. **Delivery gates** — Round 1 placed T030 as first among delivery merge prerequisites at polish; round 2 moves Accept to first delivery task after Gate 1. T031–T032 ship-review split + rebased-head verification; T028 explicitly runs `SubtitleSetupDocumentationTests`.
7. **Checklists + receipt** — CHK005, CHK019, CHK038 (and related) updated against repaired refs; Q6 `[assumed]`, DEV-525 before-Build, and existing Q12 round caps preserved.

## Analyze fix round 2 — dispositions

1. **ADR order** — T030 moved to Phase 1 immediately after separate Gate 1 (before T001/Build); plan.md task order and FR-014/SC-008/CHK010/CHK038 use “first in delivery task sequence after Gate 1”; DEV-525 Done-before-Build retained at T001; before-deliverable-merge bar retained (brief.md:19; Q11).
2. **Legacy contract evidence** — Removed unverified legacy field/type/nullability table from `contracts/search-response.md`; FR-003, CHK003, and this receipt no longer claim recon-DEV-551 substantiated exact fields; reference unchanged legacy contract; baseline parity remains in T010.
3. **Deterministic wire contract** — `empty` is required under state `ok` for settled successful empty search (not optional); status matrix covers one usable + off/refused → 200, all unusable → 409, multi-usable zero-candidate 200, single-OpenSubtitles legacy; those cases and each legacy projection assigned to T010 wire tests; T009 carries Enumeration (or equivalent) serialized by `Name`.
4. **Q7 regression proof** — T013 asserts every returned over-capacity token resolves immediately; T015 assigns later-search eviction → existing stale response with zero paid calls, explicit bounded/provider-bound property invariant, and US2 scenarios 4–6 independently of expiry; FR-008 / US2 scenarios / plan Verification updated.
5. **Generation dependencies and checklist trace** — T018 labeled **adapter contract proof**; T010 labeled **endpoint parity plus isolation**; T026 depends on both actual receipts and complete wire assertions; CHK010/019/038 and this receipt reconciled to the repaired sequence.

## Analyze fix round 3 — dispositions

1. **Status precedence** — `contracts/search-response.md` and FR-003 / Scope item 7 use disjoint first-match status rules: request-level existing → Q2 no-usable 409 → one-provider OpenSubtitles legacy (may include baseline non-200; do not invent) → multi-provider ≥1 usable 200 (one usable + off/refused; multi-usable zero-candidate). T010 and CHK027 cover all branches including legacy non-200 settled baseline.
2. **Reason determinism** — Exclusive pre-I/O precedence (disabled → `off`/`off`; else unsafe URL → `unusable`/`unsafe_base_url`; else missing/invalid credentials/timeout/gap → `off`/`provider_not_configured`) and request-time `no_search_input` only when usable with no formable input; T010 wire assertions; CHK013 and this receipt reconciled (Q2/Q4/Q5/Q8).
3. **Fallback edge** — Edge Cases no longer treat missing identifiers alone as terminal; skip unavailable earlier ID and try next available input; no formable input at all → `failed`/`no_search_input`; T011 retains both proofs (Q5; FR-006).
4. **Endpoint proof trace** — plan Verification splits search 409/wire to T010 and download 409 + read-only precedence endpoint integration to T015; T026 remains dependent on T018 + T010 search wire only; CHK040 and related checklists/receipt synced.

## Analyze fix round 4 — dispositions

1. **Complete search wire proof** — T010 explicitly includes request-level existing error branch and simultaneous-fault first-match cases; T026 required receipt lists those assertions; legacy assertions remain against unchanged baseline; plan Verification and CHK027 aligned.
2. **Settlement / usability freeze** — Applied Keel ruling (`brief.md:69`, `CONCLUSIONS.md:111`, note DEV-551:36): usability frozen at request-time acquisition configuration validation; classified off/unusable rows settle pre-I/O; Q2 search 409 always includes ordered ProblemDetails `providers`; request-level errors omit the key; FR-003/FR-004, contracts, Edge Cases, CHK027/CHK031 aligned; T010 asserts presence/order on Q2 409 and absence on request-level.
3. **Phase 10 receipt chain** — T031/T032 wait for all in-scope receipts including T015 download integration, T024 health, and T028 setup documentation; plan Gates and CHK038 synced; stale “analyze fix round 2” draft labels on plan/tasks refreshed to round 4.

## Analyze fix round 5 — dispositions

1. **Discriminating Q2 usability-freeze fixture** — T010 item (10), T026 required receipt, and `contracts/search-response.md` wire-level proof list require a multi-provider configuration whose only config-usable provider settles with timeout, search 429, failure, or no formable input, with an off/refused sibling; each of the four settlements asserts **200**, **empty candidates**, and ordered named `providers` outcomes (not Q2 409). CHK027 and this receipt reconciled.

## Plan-challenge fix round — dispositions

1. **H1/M1/M2** — Dual HTTP opt-in (credential-free declaration **and** explicit typed opt-in, default disabled; credentialed refusal; OpenSubtitles HTTPS-only), response-independent destination trust with refusal before any disallowed contact, and redacted unsafe-URL warnings (provider name + `unsafe_base_url` only) carried into FR-002/FR-005/FR-011, US3, plan Verification, T003/T005/T006/T028, and CHK004/029.
2. **M3/S1** — Constitution VII subtitle acquisition ValidateOnStart exception made explicit; request-time 409 and read-only-before-acquisition clarified; T020/T021 prove invalid timing leaves host, existing tracks, and usable-sibling search available with zero calls to the invalid provider; CHK002/030 updated.
3. **M4** — FR-004 and contracts scoped to settled **search** `providers`; download error subsection pins unchanged ProblemDetails without `providers`, read-only before acquisition 409, and stale/invalid behaviour; T015 wire assertions; T026 remains search-only; CHK031/040 synced.
4. **S2/S3** — Core Enumeration + JSON Name conversion and centralized ProblemDetails required in plan/T009/T014; T001 inventories concrete reuse seams and stops for Q10 Patron file ruling if the approved envelope is insufficient; CHK003/013/033 updated.
5. **S4** — Core-side lookup owns host-lifetime per-provider spacing and observation state; T004/T020/T023/T024 cover concurrent spacing/cancellation and search/health sharing with instance-owned state; typed-client composition remains outside Core; CHK007/008 updated.
6. **S5** — Replaced draft/Quill-named attribution with Patron approval for closed state/reason codes and `Enabled`/`MinimumRequestGap` throughout spec/plan/contracts/checklists/receipt (CONCLUSIONS.md S5; ASSUMPTIONS.md).

## Plan-challenge fix round 2 — dispositions

1. **Split 409 predicates (Ledger S1 / ADR 0052)** — Scope item 11 and FR-002 now separate **search** (all enabled providers; 409 only when none usable; usable sibling keeps search configured) from **download** (bound provider after read-only; 409 even with usable sibling; zero calls to either). T014/T015 and plan Verification require the bound-provider case plus legacy status/ProblemDetails-without-`providers` on 409, read-only, and stale/invalid paths. US1.7 moved to usability-freeze; US2.8–9 cover download 409 / read-only / stale-invalid. CHK002/CHK040 and SC-005 reconciled.
2. **Four independent HTTP gates (T006 / CHK029)** — Typed-client suite and checklist prove permit credential-free+opt-in, refuse missing opt-in, refuse missing credential-free declaration despite opt-in, and refuse credential/login despite both opt-ins. Plan Verification and US3/scenario 5 aligned.
3. **Private TASKS mirror** — Prerequisite-resolved private TASKS copy refreshed from canonical `specs/DEV-551/tasks.md` and verified byte-identical (also mirrored `plan.md`).

## needs decision

None. Plan-challenge fix round 1 (note DEV-551:47–53) and round 2 (note DEV-551:55–58) closed from Keel’s fix lists, `CONCLUSIONS.md` Ledger S1 / ADR 0052, and `brief.md` Phase 2 plan-challenge decisions.

## Notes

- `before_specify` hook (`grill-with-docs`) already satisfied by closed grill artifacts; not re-run.
- Prerequisite-resolved private mirrors: Spec Kit resolves IMPL_PLAN/TASKS from the private ticket directory. Paths: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-551\plan.md` and `...\tasks.md`. Quill keeps canonical `specs/DEV-551/plan.md` and `specs/DEV-551/tasks.md` authoritative and mirrors both so private and worktree copies stay byte-identical.
- No `research.md` / `data-model.md` / `quickstart.md` — grill freezes inventable unknowns; no schema change; additive API contract under `contracts/`.
- No structural owner checkbox. Gate 1 / PLAN FROZEN / proof PASS / integrated acceptance / hosted merge readiness not claimed (Gate 1 remains **pending**). ADR 0052 stays Proposed on Spec PR until T030 Accepts it first in the delivery sequence after Gate 1 (Q11; brief.md:19).
- ui:none — Nothing visible; DEV-552 owns picker consuming `providerId`.
- Serialize with DEV-550 on `SubtitleProviderServiceCollectionExtensions.cs` noted in plan/T001; not a scope expansion.
- Existing `RequestTimeout > TimeSpan.Zero` and binder 1000/five-minute defaults reused as already-ruled facts (Q7–Q8).
- Preserved: Q6 `[assumed]` ordering in `ASSUMPTIONS.md`; DEV-525 Done-before-Build at T001; existing round caps (two plan-challenge / two implementation review; ≤2 fix commits per round).
