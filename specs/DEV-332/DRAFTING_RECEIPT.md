# DEV-332 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-29
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-332-spec` · branch `feature/332-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask with `$env:PYTHONUTF8=1`. `setup-plan.ps1 -Json` resolved `specs/DEV-332` (plan stub then filled). `setup-tasks.ps1 -Json` after `spec.md` existed. `feature.json` already pointed at `specs/DEV-332` and was left unchanged. `before_specify` grill-with-docs skipped (grill already closed; brief present). `after_tasks` `/speckit-analyze` left for Keel per charter/pipeline.

**Inputs**: frozen `specs/DEV-332/brief.md` (Q1–Q10; zero unresolved design questions; zero owner structural checkboxes), `CONCLUSIONS.md`, draft `docs/adr/0031-local-aspire-apphost.md` (read-only; Quill does not author ADRs), Maestri notes `recon-DEV-332`, `recon-DEV-332-2`, `recon-DEV-332-3`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates only; no edits).

**Artifacts written**:

- `specs/DEV-332/spec.md`
- `specs/DEV-332/plan.md`
- `specs/DEV-332/tasks.md`
- `specs/DEV-332/checklists/requirements.md`
- this receipt

**Owner checkbox**: none (Q10).

**Decisions invented by Quill**: none. Technical behaviour taken only from brief/CONCLUSIONS/ticket. Drafting-only layout choice: optional IntegrationTests files named `AppHostOrchestration*` if T017 is needed (plan Project Structure); brief approved additive IntegrationTests edits without naming files.

**needs decision:** none.

**Verification**: not run (Phase A drafting only; no build/test gates).

---

## Analyze fix round 1 (Keel → Quill) — 2026-10-29

Applied Keel numbered drafting fixes only; no new design decisions, implementation, or gates.

1. **MEDIUM coverage — Phase B pickup prerequisite**: Added unchecked Phase B pickup section before T001 in `specs/DEV-332/tasks.md` (main/contract/file drift check + re-analysis; all T001–T025 depend on it). Updated Gate 1 wording, Phase Dependencies, and MVP accordingly. Task IDs T001–T025 preserved.
2. **MEDIUM coverage — solution build verification**: Added `T026` in Phase 9 of `specs/DEV-332/tasks.md` (build `LamuFlix.sln` + exit receipt before T018–T025); marked T018–T025 as depending on T026; noted Phase 2 shell-build checkpoint is insufficient. Updated Phase Dependencies, MVP, Notes.
3. **MEDIUM scope consistency — Postgres local/dynamic host mapping**: Carried brief.md:38 host-mapping rule into `specs/DEV-332/spec.md` FR-004 + Postgres Key Entity, `specs/DEV-332/plan.md` Postgres approach bullet, `specs/DEV-332/tasks.md` T007 and T016 real-host acceptance receipt. No new API; scope not widened.
4. **LOW path consistency — ADR location**: Corrected Project Structure diagram in `specs/DEV-332/plan.md` so `docs/adr/0031-local-aspire-apphost.md` sits at repository `docs/adr` (Keel ownership; proposed), not nested under `specs/DEV-332`.

**Saved locations**: `specs/DEV-332/tasks.md`, `specs/DEV-332/spec.md`, `specs/DEV-332/plan.md`, `specs/DEV-332/DRAFTING_RECEIPT.md` (this append).

---

## Plan challenge fix round 1 (Keel → Quill) — 2026-10-29

Applied `PLAN_CHALLENGE.md` Round 1 items 1–8 plus recorded PC1/PC2. No Keel-owned edits (`CONCLUSIONS.md`, `brief.md`, ADR, `PLAN_CHALLENGE.md`). No PLAN FROZEN. No production edits/tests/gates. Round 2 not begun.

### PC1 (Patron) — included in this receipt

- No constitution amendment; no owner §2.3(b) structural checkbox.
- Cited Patron rationale in `plan.md` Constitution Check and `spec.md` Assumptions/FR-017: Constitution I:178–197 (AppHost outer launcher, one-way Api metadata); Tech Stack:393–416,588; Governance/Docs:559–561,609.
- ADR 0031 remains `proposed` now; Keel marks `Accepted` at plan freeze before Gate 1 (explicit in spec FR-017, plan Docs/ADR row + Ordering, tasks Gate 1 header).
- No ServiceDefaults duplication; Api Projects metadata reference only.

### PC2 (Patron) — included in this receipt

- Replaced every Quill-owned old Q7 exclusion reason with exact: `local orchestration wiring; real-host acceptance receipts required; Core mutation retained per Constitution IX`.
- Locations updated: `spec.md` US6.4 / FR-013 / SC-005; `plan.md` Summary / Constitution IX / Approach Harness; `tasks.md` T004 / T024 / Notes.
- Meaningful automated IntegrationTests remain optional (T017); thresholds, Core mutation, and blocking real-host acceptance unchanged.
- Historical Q7 text left intact in append-only Keel files (`CONCLUSIONS.md`, `brief.md` Q7 row); Quill did not edit those.

### Numbered Round 1 items — read-back

| # | Item | Applied to | Read-back |
|---|---|---|---|
| 1 | Compass H1 AppHost-owned Api HTTP endpoint | `spec.md` US4/FR-010; `plan.md` Api wiring; `tasks.md` T008/T011 | Same endpoint for `/health/ready` and `VITE_API_BASE_URL`; no Api file changes; exact binding API marked recon-pending |
| 2 | Sentry M1/M4, Compass M2 healthy-before-dependent + cold-start migration | `spec.md` US1/US2/FR-005/010; `plan.md` Approach/Test; `tasks.md` T008/T011/T016 | Genuinely healthy Postgres/RabbitMQ before Api; healthy Api before web; T016 cold-start migration before Api serves; no unverified WaitFor claims |
| 3 | Sentry M3, Compass M1/M3 WithReference + AMQP port + no management UI | `spec.md` US3/FR-006; `plan.md` RabbitMQ; `tasks.md` T009/T010 | Ticket WithReference retained (DEV-332:28); integer-compatible mapped AMQP host port + equality/success proof without credential operands; AMQP only; resolved-port API recon-pending |
| 4 | Sentry M2, Ledger M3 no-shell argument-list launch | `spec.md` US4/FR-009/Edge; `plan.md` Web; `tasks.md` T014/T016 | Forbid cmd.exe/shell/concat; argument-list; deterministic repo-relative web dir; separate Windows/proxy/occupied-5173 proofs; no npm.cmd prescription |
| 5 | Sentry L1/L2/H1/H2 strengthened T016 receipts | `spec.md` FR-008/014; `tasks.md` T012/T016/T015 | Full pin vs running/configured image; timing inequality + positive sweep; missing-prerequisite failure; sanitized receipts under `specs/DEV-332`; secrets-flow/API recon-pending retained |
| 6 | Sentry M4, Ledger M1 optional T017 constraints | `spec.md` FR-014; `plan.md` Proof/Structure; `tasks.md` T017 | Optional + feasibility/skip-with-reason; no new package/project; exclusive serial 5173 if host-starting; `AppHostOrchestration*` proposed name only; H2 closed by PC2 |
| 7 | Ledger M2/L3, Compass L2 CPM/analyzer/allowlist | `spec.md` FR-012/015; `plan.md` Gates; `tasks.md` pickup/T002/T018–T021/T023/T026 | CPM clean restore/build; transitive vulnerable-package; changed-source excludes obj/generated; no ServiceDefaults duplication; allowlist at pickup + Phase 9 entry; full format retained |
| 8 | Compass L3 / Ledger L1 T026 in pickup ranges | `tasks.md` pickup/Notes/Phase deps | Stable IDs T001–T026; pickup depends for T001–T026; T026 before T018–T025; no renumber; Q7/constitution status shows PC1/PC2 rulings |

### Unresolved dependencies retained (not invented)

From `PLAN_CHALLENGE.md` Decision/recon dependencies — still open blockers in spec Edge Cases, plan Risks, and task notes:

1. Pinned 13.6.4 pgvector full-digest override API
2. Physical DB `lamuflix` versus `DefaultConnection` resource/connection alias
3. Postgres/RabbitMQ health checks and WaitFor healthy semantics
4. AppHost-owned Api HTTP endpoint/binding without Api launchSettings
5. Development Api user-secrets loading without copying values
6. Supported no-shell Windows npm launch / proxy-disable / web path mechanisms
7. Integer RabbitMQ host-port expression API
8. SDK implicit AppHost reference / CPM interaction
9. Generated credential ownership/persistence / UserSecretsId requirements

### needs decision

none for Quill drafting (PC1/PC2 recorded by Keel). Recon facts still required before closing related findings.

### Saved locations

`specs/DEV-332/spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md` (this append).

---

## Plan challenge fix round 2 (Keel → Quill, cap 2) — 2026-10-29

Applied `PLAN_CHALLENGE.md` Round 2 items 1–8 using PC3/PC4 + brief recon construction mapping + recon-DEV-332-4/-5. Preserved Round 1 / analyze / PC1/PC2 history. No Keel-owned edits (`CONCLUSIONS.md`, `brief.md`, ADR, `PLAN_CHALLENGE.md`). No PLAN FROZEN. No production edits/tests/gates/new packages. No third round.

### Numbered Round 2 items — saved passage references

| # | Item | Applied to (saved passages) | Disposition |
|---|---|---|---|
| 1 | Sentry H1 WithImage + separate WithImageSHA256 full compose digest | `spec.md` Scope / US2.1 / FR-004 / Edge; `plan.md` Summary / Storage / Approach Postgres / Test 1 / Risks; `tasks.md` Conventions / T007 / T016 | Digests API named; never whole `image@digest` to WithImage; representation/composed/running identity = T016 only |
| 2 | Sentry H3 AddDatabase(name DefaultConnection, databaseName lamuflix) + WithReference(database) | `spec.md` Scope / US2.1–2 / FR-004–005 / Key Entities; `plan.md` Storage / Approach Postgres; `tasks.md` T007 / T008 | Alias vs physical name separated; no rename |
| 3 | Sentry M1/M4, Compass M2 WaitFor Healthy not WaitForStart; WithHttpHealthCheck | `spec.md` US1.1 / FR-005 / FR-010; `plan.md` Approach Api/Postgres/RabbitMQ / Test 3; `tasks.md` T008 / T010 / T011 | Built-in checks; cold-start/four-resource timing = T016 |
| 4 | Compass H1, Sentry H2 WithHttpEndpoint(http) + PC4 Development env | `spec.md` Scope / US4.2 / US6.2 / FR-010 / Assumptions; `plan.md` Approach Api / Credentials; `tasks.md` T008 / T011 / T015 / T016 | Explicit http endpoint reuse; no Api launchSettings/code/copy; plain-HTTP blocks Phase B |
| 5 | Sentry M2 PC3 PATH-resolved node + Vite bin args | `spec.md` Scope / US4.1 / Edge / FR-009 / Key Entities; `plan.md` Summary / Approach Web / Test 5 / Ordering; `tasks.md` Conventions / T014 / Notes | Active npm launch prescriptions replaced; npm install prerequisite retained; live Windows/proxy/occupied/browser = T016 |
| 6 | Sentry M3, Compass M1 PrimaryEndpoint.Property(Port) IExpressionValue | `spec.md` US3.1 / FR-006; `plan.md` Approach RabbitMQ / Test 2; `tasks.md` T010 / T016 | Host separate; no URL/TargetPort for int port; formatting/equality = T016 |
| 7 | Ledger M2 explicit AppHost ref suppresses implicit; CPM PackageVersion | `spec.md` FR-015; `plan.md` Approach Project/pins / Risks; `tasks.md` T001 / T002 | CPM uncertainty removed; restore/build + transitive vuln still Phase B unrun |
| 8 | Ledger L2 PC4 AppHost UserSecretsId; T006 single-source; broker UserNameReference/PasswordParameter | `spec.md` Scope / US3.2 / FR-007 / Assumptions; `plan.md` Approach Project/pins + Credentials; `tasks.md` T001 / T006 / T009 / T010 / Notes | Duplicate T006 params removed; recon-pending blockers replaced with mechanisms; Phase B proofs accurately separated; PC1/PC2 + ADR proposed-now/Accepted-at-freeze preserved |

### Retained blocking Phase B proofs only (unexecuted)

Digest representation/composed/running image identity; actual child Api secret loading under explicit Development; Windows node/Vite + proxy-disabled + occupied-port + browser; RabbitMQ mapped-port numeric formatting/equality + Api validation/readiness; clean CPM restore/build + transitive vulnerable-package checks. No plan-time claim those passed.

### needs decision

none for Quill drafting (PC3/PC4 recorded by Keel/Patron).

### Saved locations

`specs/DEV-332/spec.md`, `plan.md`, `tasks.md`, `DRAFTING_RECEIPT.md` (this append).
