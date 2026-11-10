# DEV-453 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-09
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-453-spec` · branch `feature/453-spec`
**HEAD at drafting**: `cd575e55` (grill-closed brief/CONCLUSIONS/ASSUMPTIONS + ADR 0038)
**Method**: Spec Kit drafting in one ask (`/speckit-specify`, `/speckit-clarify`, `/speckit-checklist`, `/speckit-plan`, `/speckit-tasks`). Grill already closed (`brief.md` Q12); mandatory `before_specify` grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge). Analyze round 1 repairs applied from Keel numbered fix list (C1–C4, I1). Analyze round 2 repairs applied from Keel final bounded fix list against brief Authoritative analyze adjudication addendum + corrected Patron CONCLUSIONS. Plan Challenge round 1 planning repairs applied from Keel single numbered repair list 1–11 against brief Authoritative Plan Challenge adjudication addendum (2026-11-10) / note `adjudication-DEV-453-Keel`. `.specify/feature.json` already pointed at `specs/DEV-453` and was left unchanged. `$env:PYTHONUTF8=1` set for the session. `setup-plan.ps1 -Json` and `check-prerequisites.ps1 -Json -PathsOnly` run before drafting.

**Inputs**: grill-closed `specs/DEV-453/brief.md` (Q1–Q12) + Authoritative analyze adjudication addendum + Authoritative Plan Challenge adjudication addendum (2026-11-10); `CONCLUSIONS.md` corrected Patron analyze rulings; `ASSUMPTIONS.md`; Keel-authored `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md` (Proposed); Maestri notes `DEV-453`, `recon-DEV-453`, `adjudication-DEV-453-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only).

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-453/spec.md`
- `specs/DEV-453/plan.md`
- `specs/DEV-453/tasks.md`
- `specs/DEV-453/checklists/requirements.md`
- this receipt
- repository-root `CONTEXT.md` (append-only five settled terms only — authorized by brief addendum C4/VIII / Patron corrected item 4)

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR bodies (including `docs/adr/0038-subtitle-provider-and-sidecar-boundary.md`), constitution, production/test code, `harness.yml`, `README.md`, Compose files, generated OpenAPI/TS.

**Owner checkbox**: **unchecked structural C4/VII** copied exactly from brief Authoritative analyze adjudication addendum item 3 into `spec.md` Owner question — Gate 1 and preserved **verbatim** through Plan Challenge round 1 (including `SubtitlesOptions / OpenSubtitlesOptions` wording). Spec-PR body handoff MUST carry the same unchecked question. Gate 1 stays closed until user approval. Quill does not check it and does not amend the constitution on assumed approval. Clarifying implementation-name note added beside the checkbox only: C# types are functionality-named `SubtitlesOptions` / `SubtitleProviderOptions`. If/when approved, scoped Principle VII amendment must also append new subtitle options record(s) to the closed options-record list. Affected ValidateOnStart acquisition-options implementation remains blocked until owner approval + scoped constitution/governance amendment lifecycle. Preserve Q10 / no numeric defaults. Principle I remains independent.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ASSUMPTIONS/ADR 0038, recon facts, and the cited analyze + Plan Challenge adjudication addenda. Round 2 removes obsolete C1/C2/VIII placeholders and keeps C4/VII visibly unresolved with citations. Plan Challenge round 1 applies Keel list 1–11 only; no PLAN FROZEN / analyze-clean claim; Quill does not start another challenge round.

## Encoding notes (not new decisions)

- No `research.md` / `data-model.md` / `quickstart.md` / `contracts/` — decisions frozen in brief; routes/DTO/statuses live in spec + generation tasks (precedent DEV-456 / DEV-438).
- Brief-relative routes (`/movies/{id}/subtitles/...`) with raw `{id}` capture/parse are the approved public relative contract (Plan Challenge item 2 / Sentry F3). Exact existing DI/endpoint-mapping filenames and any MapGroup prefix are deferred to Phase B Wisp T001 — `recon-DEV-453` does not retain `ApiEndpoints.cs` or `/api` grouping as facts (analyze I1).
- Options nesting: `DownloadAllowedHosts` / `MaxDownloadBytes` / `RequestTimeout` under vendor-mandated `Subtitles:OpenSubtitles` as brief-authorized; C# types functionality-named `SubtitlesOptions` / `SubtitleProviderOptions` (Plan Challenge item 4).
- Principle I residual compliance for the two brief-approved ports (`ISubtitleProvider`, `ISubtitleSidecarStore`) is modeled as Keel-owned constitution enumeration/governance/feature-folder compliance + atomic ArchitectureTests whitelist append — same pattern as DEV-456 / ADR 0037 — without inventing MINOR version numbers beyond “appropriate MINOR”. Quill does not backfill historically omitted ports. Principle I remains independent of blocked C4/VII. Provider port is data-only; hash uses authorized read-only `ILibraryFileReader` (Plan Challenge item 5).
- Principle II/V scheduled and aligned to resolved C1/V + Plan Challenge: sealed record commands/queries, sealed Core handlers, AddHandler + decorators, FluentValidation, thin TypedResults endpoints, single ProblemDetails mapper with `traceId`, typed Core failure categories only. Transport malformed-id **400** via raw capture/parse; well-formed absent **404**; FluentValidation **422** retained for reachable validators; application-validation 422 metadata/OpenAPI/tests required; do not claim all validation is 400; no invented GET 422.
- Principle VI scheduled and aligned to resolved C2/VI + Plan Challenge L1: functionality-named TelemetryConstants, movie-id structured events, ServiceDefaults/Infrastructure-owned wiring, ONE always-Healthy optional-provider local health policy with sanitized `ready` / `disabled_or_unconfigured` metadata, zero network/quota calls, full enabled/disabled/configured/invalid matrix, **excluded from `/health/live`**; no reachability claims; no other-dependency health changes.
- Principle VIII planning compliance: append-only root `CONTEXT.md` five-term edit performed earlier / same spec PR (authorized; not deferred to delivery). Supersedes prior feature-local-only / no-root-edit claims in Quill drafts. `plan.md` diagram places `CONTEXT.md` at repository root (Ledger L2).
- Exact DI/endpoint mapping filenames, hash reference vectors, download-host allowlist strings, Movie primary-title property, `ILibraryFileReader` head/tail contract, download raw/wrapper shape, feature-folder enumeration/isolation, and current provider header/error schema remain Phase B Wisp verification inputs (brief Q9–Q10; Plan Challenge items 1, 5–6, 9–10).
- UTF-8 ≥ / arrows in `spec.md` preserved intact (Ledger L4 not reproduced; no encoding rewrite). ADR 0038 Proposed remains Keel-owned accepted historical context (Ledger L3); no Quill ADR rewrite.
- Runtime receipt path uses conventional `specs/DEV-453/runtime-acceptance.md` (sibling precedent DEV-456/DEV-457) to hold the four brief-required live evidences; not a Patron taste ruling.
- Local `tasks.md` Phase N IDs are for `/implement Phase N` dispatch and are not canonical task-pipeline phases.
- `after_tasks` harness hook would chain `/speckit-analyze`; Quill did not execute it — Keel owns analyze / plan challenge.
- Does **not** claim Gate 1, PLAN FROZEN, analyze-clean, runtime acceptance, or merge clearance. This is Plan Challenge round 1 planning repairs only; two-round cap remains; Quill does not start another challenge round.

## Clarifications (`/speckit-clarify`)

- Interactive clarify after grill closure: **no critical ambiguities** requiring stakeholder questions. Clarifications session recorded in `spec.md`.
- Phase B verification items (dependencies, DI filenames, hash vectors, allowlist, provider schema) are brief Q9 tasks, not open product decisions for Quill to fill.

## Checklist (`/speckit-checklist`)

- Focus: requirements quality for provider/sidecar/API/safety/gates/runtime.
- Depth: Standard.
- Audience: Keel analyze / plan challenge + Gate 1 reviewers.
- File: `specs/DEV-453/checklists/requirements.md` (CHK001–CHK028 after analyze round 2; CHK029–CHK035 added for Plan Challenge round 1 coverage).

## Analyze round 1 repairs (Keel fix list)

Applied to Quill-owned `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, and this receipt only. Brief/CONCLUSIONS/ASSUMPTIONS/ADR/constitution remained read-only. No source recon run.

## Analyze round 2 repairs (Keel final bounded fix list)

Applied against brief Authoritative analyze adjudication addendum (recorded first) and corrected Patron answer in CONCLUSIONS.md:

1. C1/V — aligned all five draft artifacts to transport malformed-ID **400** vs validly-bound FluentValidation **422**; retained explicit routing; single ProblemDetails+traceId mapper; application-validation 422 metadata/OpenAPI/tests; no “all validation is 400” claim.
2. C2/VI — aligned C2/T012b to ONE always-Healthy optional-provider local health policy with sanitized `ready`/`disabled_or_unconfigured` metadata, zero network/quota calls, full enabled/disabled/configured/invalid matrix, TelemetryConstants + movie-id events; no reachability claim; no other-dependency health changes.
3. C4/VII — remains `blocked: structural`. Exact owner question from brief addendum copied as unchecked `spec.md` checkbox; Gate 1 closed; PR-body handoff must carry it; removed no-owner-checkbox / no-departure claims; affected ValidateOnStart implementation blocked until approved scoped constitution amendment; never checked / never assumed; Q10/no numeric defaults preserved; Principle I independent.
4. C4/VIII — append-only five settled term/definition/_Avoid_: entries added to repository-root `CONTEXT.md` preserving prior content; prior feature-local-only / no-root-edit claims realigned; same-PR Principle VIII compliance (not deferred).
5. Removed obsolete needs-decision placeholders for C1/C2/VIII; kept C4/VII visibly unresolved with citations.

No production or constitution edits. No commit required by this ask. No analyze-clean / freeze / Gate 1 claim.

## Plan Challenge round 1 repairs (Keel numbered list 1–11)

Applied to Quill-owned `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, and this receipt only. Brief/CONCLUSIONS/ASSUMPTIONS/ADR/constitution/production/generated remained read-only. Owner checkbox unchanged/unchecked. No PLAN FROZEN / analyze-clean claim. Existing C4/VII remains blocked structural; Gate 1 closed. Ledger L3 accepted historical context; L4 not reproduced (UTF-8 preserved). No new challenge round started.

1. Sentry F1/F2 — pre-request HTTPS/verified host, redirects disabled, all 3xx→502, 409 vs 502 BaseUrl distinction, T001 STOP, never-contacted tests.
2. Sentry F3 — raw `{id}` capture/parse, true noninteger 400 + well-formed absent 404, independent 422.
3. Sentry F4/F5 — GET nosniff + attachment basename headers; caller abort vs provider timeout; no rollback after committed save.
4. Sentry F6 — accepted double-download + crash-orphan risks recorded; no lock/janitor.
5. Ledger M1/M2 — verbatim checkbox; functional options/telemetry names; VII amendment includes options-record enumeration.
6. Ledger M3/M4 — data-only provider + `ILibraryFileReader` hash seam; T001/T001c feature-folder/isolation STOP; typed Core failures via single mapper.
7. Ledger L1/L2 — `/health/live` exclusion; root `CONTEXT.md` diagram placement; no ADR/UTF-8 rewrite.
8. Compass F1/F2 — scenario renumber 7→8; `selectedLanguage` always on saved / null otherwise.
9. Compass F3 — T001 Movie primary-title verification; blank title → noMatch.
10. Compass F4 — full minimal downloaded-SRT criteria + raw/wrapper STOP; existing-track exact bytes unchanged.
11. Compass F5/O1 — Retry-After delta-seconds/HTTP-date; reachable 422 only; STOP on unresolved conflict.

## needs decision

- needs decision: C4/VII — `blocked: structural` owner checkbox (exact brief addendum wording in `spec.md` Owner question — Gate 1; must also appear unchecked on the spec-PR body). Cited: brief Authoritative analyze adjudication addendum item 3; brief Authoritative Plan Challenge adjudication addendum items 3–4; CONCLUSIONS.md corrected Patron item 3; constitution Principle VII:500-503/625-626; Q8/Q10; charter §2.3(b). Gate 1 closed until user approval. Do not implement or amend constitution on assumed approval; if approved, require scoped constitution/governance amendment (ValidateOnStart departure **and** options-record enumeration append) before affected ValidateOnStart-departure implementation. Preserve Q10 / no guessed numeric defaults. Principle I remains independent.

## needs recon

- Exact existing DI registration and endpoint-mapping filenames (and any MapGroup prefix) remain brief-delegated Phase B Wisp facts (Q10) — not product decisions Quill may fill; Phase A no longer asserts `ApiEndpoints.cs` or `/api` grouping.
- Authoritative hash reference vectors, exact API/download hosts + protocol compatibility (redirect requirement STOP), download-host allowlist contents, Movie primary-title property, `ILibraryFileReader` head/tail contract, download raw/wrapper payload shape, feature-folder enumeration/isolation rules, and current OpenSubtitles header/error schema remain Phase B verification (Q8–Q9; Plan Challenge items 1, 5–6, 9–10).
- ArchitectureTests whitelist current entry count/pattern for append-only insertion of the two new ports is refreshed at Phase B pickup (T001) before T002 code lands.
