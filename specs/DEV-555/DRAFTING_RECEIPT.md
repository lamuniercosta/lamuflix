# DEV-555 drafting receipt (Quill)

**Date**: 2026-11-28
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-555-spec`
**Branch**: `feature/555-spec` (verified)
**HEAD at draft**: `252f1342b194150a27581000d2b91e6ab0802006`
**PYTHONUTF8**: 1

## Commands run

- `$env:PYTHONUTF8='1'` then `.specify/scripts/powershell/setup-plan.ps1 -Json` (resolved FEATURE_SPEC / IMPL_PLAN / PRIVATE_TICKET_DIR)
- `/speckit-specify` — wrote `specs/DEV-555/spec.md`; quality checklist `checklists/requirements.md`
- `/speckit-clarify` — recorded closed from grill in `spec.md` Clarifications (no interactive questions; no critical ambiguities)
- `/speckit-plan` — wrote `specs/DEV-555/plan.md` (+ mirrored to private Spec Kit IMPL_PLAN); `contracts/bsplayer-soap-adapter.md`
- `/speckit-tasks` — wrote `specs/DEV-555/tasks.md` (+ mirrored to private Spec Kit TASKS)
- Supporting: this receipt
- Omitted `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q5 freeze inventable unknowns; no schema; adapter contract under `contracts/`
- Did not edit `brief.md` / `CONCLUSIONS.md` / `ASSUMPTIONS.md` (Keel/Patron owned)
- Did not edit `CLAUDE.md` Spec Kit plan pointer (outside ticket file scope / care item 6)
- `before_specify` hook (`grill-with-docs`) already satisfied by closed grill artifacts; not re-run

## Repair round 1 (Keel analyze r1)

**Source**: `C:/Users/lamun/AppData/Local/LamuFlix/artifacts/LamuFlix/DEV-555/receipts/keel-speckit-analyze-r1.md` (11-item Quill repair list).
**Applied to**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/bsplayer-soap-adapter.md`, this receipt; private `plan.md` / `tasks.md` mirrors synced byte-identical.

| # | Severity | Disposition |
|---|---|---|
| 1 | HIGH | Options field set: XML size bound, max download bytes, download-host allowlist added to Key Entities, FR-002, Scope item 2, T004, contract Options table, T011; allowlist may equal server list when T003 evidence shows downloads from those hosts |
| 2 | MEDIUM | Plan VI/VIII, T012, contract Health/telemetry: functional health/telemetry/`TelemetryConstants` names; reuse DEV-551 provider-identifier mechanism; vendor-named constant → STOP for Patron |
| 3 | MEDIUM | T002 (+ contract): capture failure → STOP blocked with attempt evidence; never synthesize from HolyArrs |
| 4 | MEDIUM | T002/T008 (+ contract/Key Entities): regenerable deterministic test-generated / tiny non-media known file; no copyrighted media |
| 5 | MEDIUM | T001 item (3) home-settings surface (tracked vs generator); T004 home enablement within necessary appsettings; STOP if generator |
| 6 | MEDIUM | FR-004 + T009 (+ Scope 4 / plan Verification / contract Deadline): hanging attempt ends before whole remaining deadline while untried servers remain; no invented numeric share |
| 7 | LOW | Plan Project Structure, T001/T011, contract Fixtures: `SubtitleSetupDocumentationTests`, README, `docker/api.settings.example.json`, `tests/fakes/bsplayer/` marked T001-cited pickup facts |
| 8 | LOW | T003 evidence note location named: PRIVATE_TICKET_DIR/receipts/ |
| 9 | LOW | Removed T004 `[P]`; Parallel Opportunities bullet corrected (sequential after T001+T003) |
| 10 | LOW | T009: invalid/disabled BSPlayer — existing-track and languages routes still serve (SC-004) |
| 11 | LOW | Plan VIII STOP: new domain term in code requires CONTEXT.md in same PR → outside Q4 → Patron ruling first |

## Repair round 2 (Keel plan-challenge r1)

**Source**: `C:/Users/lamun/AppData/Local/LamuFlix/artifacts/LamuFlix/DEV-555/receipts/keel-plan-challenge-r1.md` (16-item Quill fix list). Authority: `brief.md` § Plan-challenge decisions (r1) D1–D15; `CONCLUSIONS.md` § Plan challenge r1 adjudication.
**Applied to**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/bsplayer-soap-adapter.md`, this receipt; private `plan.md` / `tasks.md` mirrors synced byte-identical.

| # | Decision | Disposition |
|---|---|---|
| 1 | D11 Options shape | Named `SubtitleProviderOptions` instance from `Subtitles:BsPlayer`; reuse `RequestTimeout`/`DownloadAllowedHosts`/`MaxDownloadBytes`/`Enabled`/`AllowPlainHttp`; add only `Servers`/`MaxResponseBytes`; OpenSubtitles validation byte-identical; download-cap precedence; STOP if named instance impossible; drop “server list itself when T003…” branch; allowed hosts = union of `Servers`+`DownloadAllowedHosts`; empty `DownloadAllowedHosts` valid; T001 cites DEV-551 options shape/binding/download-cap |
| 2 | D1 Attempt budget | Formula + close gets no reserved budget in FR-004, contract Deadline/Session, T005; T009 D1 tests (≥3 hanging/refusing/answering; zero-budget close success) |
| 3 | D2 Session scope | Per-call token + stateless adapter in FR-004/T005/contract Session; concurrent two-search test in T009 |
| 4 | D3 Destination gate | New contract “Destination predicate” row; referenced from FR-003/T005; D3 table tests in T009; H3(e) DNS-rebinding exclusion in Edge Cases (Q2) |
| 5 | D4 Cancellation | Linked-token classification in contract Cancellation + T005; T009 caller-cancel-during-probe case; T001 cites DEV-551 cancellation semantics |
| 6 | D5 XML | Contract XML row = D5 settings (`XElement`/`XmlWriter`, counting-stream abort, `XmlReader` settings, `MaxResponseBytes`); T009 over-bound + internal-entity cases |
| 7 | D6 Hash divergence | Separately named routine branch in FR-009/contract Hash/T005; T008 golden/unchanged OpenSubtitles checks |
| 8 | D7 Inconclusive capture | Second-capture branch in T002/T003/contract prerequisites; hash-acceptance in SC-002/T008; scrubbed bodies + hash/size only; STOP if second capture inconclusive |
| 9 | D8 Deterministic time | Stub `HttpMessageHandler` + test-owned `FakeTimeProvider.Advance`; no wall-clock assertions; WireMock wire-shape only — in plan Technical Context/Verification, contract Time, T009 |
| 10 | D10 Fixtures | Removed every “(or tiny committed non-media)” option; known file generated in test only |
| 11 | D12 Identifiers | D12 table merged into plan Patron-approved identifiers; referenced from contract Health/telemetry + T012; vendor `TelemetryConstants` → `blocked: structural`; VI/VIII rows updated |
| 12 | D9 Telemetry and logs | FR-007/contract/T012 reuse existing+DEV-551 keys, no new attribute keys; host/attempt in logs only; movie id/attempt/failure category; log-scrub test in T012/T009 |
| 13 | D13 Home enablement | Removed home-settings enablement from FR-002/T001(3)/T004; T004 edits no home file; T011 full example block on `docker/api.settings.example.json` when confirmed; T017 lists operator keys for untracked home |
| 14 | D14 Static state | No-`static readonly`/no-mutable-static on Conventions + T004/T005 |
| 15 | D15 Validator rules | D15 rule table in contract Options + T004; Theory + MemberData in T004/T009 |
| 16 | L5 Recheck and hygiene | Keel Constitution Check recheck after T003 before T004; constitution line-number citations replaced with principle citations in spec/plan/tasks/contract |

## Sources used (decisions vs facts)

- Decisions: `brief.md` Q1–Q5 and D1–D15 only; `CONCLUSIONS.md`; `ASSUMPTIONS.md` (Q3 `[assumed]` configured list order)
- Facts: Maestri notes `DEV-555`, `recon-DEV-555` (verbatim Wanted / Done when / Out of scope / Where it lands / Open points / Needs; existing seam citations); `specs/PRODUCT.md` §§3–5; constitution cite-only in plan (Principles I, V–IX — no line numbers in repaired artifacts)
- Quill owns: `spec.md`, `plan.md`, `tasks.md`, `contracts/bsplayer-soap-adapter.md`, `checklists/requirements.md`, this receipt
- Keel owns: brief/conclusions/assumptions (not edited)

## needs decision

None. All 16 Keel plan-challenge r1 items applied from brief D1–D15 / CONCLUSIONS / recon facts without inventing scope.

## Interpretation notes (not new decisions)

- D11 replaces the draft preference for a nested type under `SubtitlesOptions` with a named instance of the existing `SubtitleProviderOptions` (satisfies Q1(a) closed list).
- Wire details (session sequence, 8 s wait necessity, hash identity, download-host shape) remain Q2 recorded-evidence prerequisites assigned to T002–T003, with D7 second-capture branch.
- Subtitle setup docs surfaces for T011 are T001-cited pickup facts; home enablement for live receipt is operator-set untracked home settings (D13) — T004 edits no home file.
- DEV-551 cited as dependency/contract (Needs; Q1), not as landed code at grill HEAD `6bd8be6` / drafting HEAD `252f1342`.
- Allowed hosts are always the union of `Servers` and `DownloadAllowedHosts`; empty `DownloadAllowedHosts` is valid (D3/D15) — the prior “server list itself when T003…” branch is dropped.
- CONCLUSIONS keeps its historical constitution line citations (Ledger L5); repaired Quill artifacts cite principles only.

## Named identifiers within frozen patterns

| Item | Value | Basis |
|---|---|---|
| Config section | `Subtitles:BsPlayer` (const on adapter; doubles as options name) | Wanted 1; Q1; D12 |
| Adapter | `BsPlayerSubtitleProvider` | D12 |
| Options record | Existing `SubtitleProviderOptions` (named instance) | D11; D12 |
| Validator | Extend `SubtitleProviderOptionsValidator` | D12; D15 |
| Enablement | `Enabled` (off in appsettings; operator sets in untracked home) | Wanted 1; Open point; Q1/Q5; D13 |
| Plain HTTP | `AllowPlainHttp: true` (BsPlayer only) | Wanted 1; Q1–Q2 |
| Added fields | `Servers`, `MaxResponseBytes` | D11 |
| Reused fields | `RequestTimeout`, `DownloadAllowedHosts`, `MaxDownloadBytes`, `Enabled`, `AllowPlainHttp` | D11 |
| All-down text | “BSPlayer is not answering” | Wanted 4; Q3 |
| Server order | Configured list order | Q3; `ASSUMPTIONS.md` |
| Preferred fake folder | `tests/fakes/bsplayer/` | DEV-549 layout; T001-cited pickup fact |
| Evidence note | PRIVATE_TICKET_DIR/receipts/ | Keel analyze repair 8 |
| Vendor `TelemetryConstants` | If required → `blocked: structural` | D12 |

## Notes

- Prerequisite-resolved private mirrors: Spec Kit resolves IMPL_PLAN/TASKS from the private ticket directory. Paths: `C:\Users\lamun\AppData\Local\LamuFlix\artifacts\LamuFlix\DEV-555\plan.md` and `...\tasks.md`. Canonical worktree copies under `specs/DEV-555/` remain authoritative for the Spec Kit PR; Quill keeps private mirrors byte-identical.
- No structural owner checkbox. Gate 1 / PLAN FROZEN / proof PASS / integrated acceptance / hosted merge readiness not claimed.
- ui:none — Nothing visible; DEV-552 owns picker-first behaviour if the 8 s wait is required.
