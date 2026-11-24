---
description: "Task list for DEV-565 unify provider API keys into API settings"
---

# Tasks: Unify provider API keys into API settings

**Input**: Design documents from `specs/DEV-565/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8) plus Q9, `CONCLUSIONS.md` Q1–Q9; Maestri notes `DEV-565`, `recon-DEV-565`; `specs/DEV-565/ticket-text.txt`; `specs/PRODUCT.md`.

**Status** — PLAN FROZEN. Gate 1 verified in T000. See the [Gate 1 receipt](CONCLUSIONS.md#phase-0-t000--gate-1-receipt).

**Tests**: Required (`brief.md` Task order step 1 and evidence table; FR-009–FR-011). Write/update focused checks first so they fail against current channel, then drive the cutover. Focused checks remain at T002/T003 and the T006 re-run. Gate closure requires the full .NET suite (not a focused-suite substitute) plus an explicit property-test run when applicable. No new test framework.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized boundary in `plan.md` / brief Q7/Q9; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` if a required path/fact is missing; never print/commit secrets; never auto-delete `tmdb-credentials`; never lower harness thresholds; never merge; never treat Patron as owner approval; never amend the constitution inside a read-only analyze task.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR carrying the open Q9 owner checkbox; only the **owner** approves the checkbox and merges; Keel T000 records verified owner approval + merge receipt. No Build work (T001+) before that T000 receipt.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open with Q9 checkbox; owner approves checkbox + merges; Keel records both) — blocks T001+
Phase 1   T001          (Wisp pickup recon strictly read-only; Keel reanalysis read-only; no constitution edit) — blocks T001b / T002/T003
Phase 1b  T001b         (post-approval scoped constitution amendment + Governance bump/Sync Impact Report; separate from analyze) — before T004; required before implementation merge
Phase 2   T002 ∥ T003   (US1/US2 focused regression checks first; both authored before T004/T005)
Phase 3   T004 → T005 → T006   (US1 template then US2 cutover consuming template; both complete before focused re-run; after T001b)
Phase 4   T007 → T008 → T009   (US3 parity fixtures/check + gitleaks allowlist cleanup) — before README
Phase 5   T010 → T011 → T012   (US4 README after parity/gitleaks; automated verification to green; both live receipts)
Phase 6   T013 → T014   (gates carrying hosted-check evidence + Size M three-axis review; constitution compliance before implementation merge)
```

Ordering per brief Task order / Q7/Q9. No dependency cycle. README completion follows parity and gitleaks (frozen brief step 4 after step 3). Runtime cutover (T004+) requires T001b complete.

---

## Phase 0: Spec PR, Q9 owner checkbox, and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR with the open Q9 structural checkbox; confirm **owner** approval of that checkbox and the spec PR merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; owner checkbox approved on the PR; verified user-merge receipt in `CONCLUSIONS.md`, or stop. Patron’s Q9 ruling alone is insufficient. No Build tasks before that dual receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-565 spec PR for Gate 1, carrying the open Q9 owner checkbox from `brief.md` / `spec.md`. Only the **owner** approves the checkbox and merges. After both are done, **Keel** records in `specs/DEV-565/CONCLUSIONS.md`: (1) verified owner approval of the Q9 structural checkbox, and (2) the verified Gate 1 spec-PR merge receipt. Receipt only. **Never treat Patron as owner approval.** T001–T014 MUST NOT proceed without both verified owner checkbox approval and merge (`brief.md` Q9; FR-014).

---

## Phase 1: Pickup recon (strictly read-only; blocks T001b / T002/T003)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory exact allowlist paths. No production edits. No constitution edit. No plan/tasks amendment inside this read-only step.
**Checkpoint**: Fresh recon recorded; Keel read-only reanalysis clean. Any constitution amendment or drift amendment is a **subsequent authorized step**, not part of T001.

- [X] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying the Q1/Q5/Q7 allowlist (`docker-compose.yml`, `.env.example`, `docker/api.settings.example.json`, `deploy/k8s/base/api-deployment.yaml`, `deploy/home/secrets.example.json`, `scripts/home/New-HomeCluster.ps1`, `scripts/home/_home-common.ps1`, `scripts/home/Test-NewHomeCluster.ps1`, `tests/LamuFlix.UnitTests/TmdbComposeTests.cs`, deploy-parity fixtures/check, `.gitleaks.toml`, `README.md`), noting Q9-conditional `.specify/memory/constitution.md` for the separate amendment task, and confirming `src/LamuFlix.Api/appsettings.json` / `web/e2e/run.mjs` remain verification inputs. **Keel** reanalyzes brief/spec/plan/tasks against landed main in a **strictly read-only** pass. Record the read-only reanalysis result. If authorized demonstrated drift requires plan/tasks changes, that amendment is a **separate subsequent authorized step** after this read-only recon/analyze — never conflated with T001. **STOP** `needs recon` on missing seams; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q7/Q9). Establish how both plain-startup and stubbed-flow phases of `scripts/run-compose-smoke.ps1` obtain provider settings. Treat it as read-only. If a verified ticket-forced regression requires an edit, stop for a cited Patron ruling. **Strictly read-only**.

---

## Phase 1b: Post-approval scoped constitution amendment (before runtime cutover)

**Goal**: Resolve the Principle VII current conflict via the owner-approved Q9 path without performing analyze-time edits.
**Independent Test**: Constitution lines 673–674 match the Q9 wording; Governance version bump, Sync Impact Report, and `Last Amended` updated; `.specify/templates/*` reviewed for follow-up; no new ADR.
**Order**: After T000 (owner approval + merge) and T001 (read-only recon). Completes before T004. Required for constitution compliance before the implementation merge (T014). Separate from T001 read-only analyze.

- [X] T001b **Build lane**. After T000 and T001: Amend `.specify/memory/constitution.md:673–674` only as scoped by Q9 — local `dotnet run` values remain in `dotnet user-secrets` or git-ignored `appsettings.Development.json`; containerized Compose and home-cluster deployments receive API settings and provider keys via mounted gitignored settings JSON (`docker/api.settings.json` → `/app/appsettings.Production.json`) or mounted Kubernetes Secret (`api-settings`); container environment variables supply non-API container initialization and host/container wiring. Follow Governance (`.specify/memory/constitution.md:949–960`): written proposal/rationale/impact already recorded in Q9/CONCLUSIONS; **no new ADR and no ADR supersession**; migration plan is the DEV-565 cutover/README sequence; update the Sync Impact Report and `Last Amended`. Use a MAJOR bump from the landed constitution version under Governance line 957. The amendment replaces the required transport for the affected deployments and requires migration of the old deployment configuration. Recheck the Q9 ADR citations (lines 132, 249, 911–913 and 951) and record no new or superseded ADR in the Sync Impact Report. Review templates and dependent docs and record the results, including no template change if applicable. A required edit beyond Q9 needs a cited Patron ruling. Do **not** fold this into T001. Do **not** start T004 until this task completes. Constitution compliance from this task is required before the implementation merge (`brief.md` Q9; FR-012/FR-013/FR-014; plan Constitution Check VII).

---

## Phase 2: Focused regression checks first (US1 + US2) 🎯 MVP tests

**Goal**: Assertions describe the unified settings channel before production cutover.
**Independent Test**: Focused Compose and home tests fail against pre-cutover wiring, then pass after Phase 3.
**Order**: T002 ∥ T003 after T001; both complete before either T004 or T005.

- [ ] T002 [P] [US1] **Build lane**. After T001: Rewrite `tests/LamuFlix.UnitTests/TmdbComposeTests.cs` to assert: `docker-compose.yml` mounts `docker/api.settings.json` to `/app/appsettings.Production.json` without `Tmdb__Enabled`/`Tmdb__ApiKey`; `.env.example` has no TMDb entries; `src/LamuFlix.Api/appsettings.json` keeps `Tmdb:Enabled` false with blank key; `docker/api.settings.example.json` documents placeholder-only `Omdb`, `Tmdb` (`Enabled: true`, `ApiKey: ""`), and `Subtitles:OpenSubtitles` with exact Q4 fields while preserving sibling Subtitles keys. Keep the existing test file and assertion library, use `Method_Condition_Expected` names and AAA headers, and parse tracked JSON structurally. Tracked fixture reads are in scope. Do not read real `.env` or secret values (FR-001/FR-002/FR-005/FR-007/FR-009; SC-002/SC-003; `brief.md` step 1).
- [ ] T003 [P] [US2] **Build lane**. After T001: Update `scripts/home/Test-NewHomeCluster.ps1` so stubbed cases verify generated OMDb, TMDb, and OpenSubtitles settings for present, absent, empty, and whitespace-only `TMDB_API_KEY` inputs. Assert an explicit empty `Tmdb:ApiKey` for absent or blank input and preserve the supplied nonblank value; use existing test conventions and inert fixture values; verify mounted `api-settings` path; assert absence of `tmdb.env` and of a second published Secret / `tmdb-credentials` publish (FR-004/FR-005/FR-009; SC-003; `brief.md` step 1).

**Checkpoint**: Focused checks encode the new channel (expected red until Phase 3 completes). Both authored before cutover.

---

Keep known-red focused-test commits local until T006 passes. Use `DEV-565 - {subject}` commit messages.

## Phase 3: Atomic runtime and provisioning cutover (US1 + US2)

**Goal**: Compose, example JSON, Kubernetes, and home provisioning share one settings channel.
**Independent Test**: Re-run T002/T003 suites green; non-provider settings preserved.
**Order**: After T001b (constitution amendment). T004 (template + Compose/env) → T005 (k8s + provisioning consuming the template) → T006. T005 is sequenced after T004 because home generation reads `docker/api.settings.example.json`. Independent k8s/manifest removals in T005 are not started as a parallel completion path ahead of that template dependency. Both T004 and T005 complete before T006.

- [ ] T004 [US1] **Build lane**. After T002, T003, and T001b: Update `docker/api.settings.example.json` with `Tmdb` (`Enabled: true`, `ApiKey: ""`) and full placeholder `Subtitles:OpenSubtitles` per Q4; preserve OMDb and sibling Subtitles keys. Update `docker-compose.yml` to remove `Tmdb__Enabled`/`Tmdb__ApiKey` while keeping the settings mount and non-provider env. Update `.env.example` to remove TMDb entries only (FR-001/FR-002/FR-005/FR-007; `brief.md` step 2).
- [ ] T005 [US2] **Build lane**. After T004 (template available) and T003: Update `deploy/k8s/base/api-deployment.yaml` and `deploy/home/secrets.example.json` to remove TMDb env entries and `tmdb-credentials` while retaining `api-settings` mount/declaration. Update `scripts/home/New-HomeCluster.ps1` and `scripts/home/_home-common.ps1` to ingest the three keys from `secretSourcePath`, write them into generated API settings from the T004 template (inherit template `Tmdb:Enabled: true`; retain explicit empty `Tmdb:ApiKey` for absent, empty, or whitespace-only input and preserve the supplied nonblank value), publish `api-settings` only, retire `Get-TmdbKeyFile` / `tmdb.env` / second Secret publish. Retain the legacy Secret for operator-managed rollback and post-verification cleanup. After each publication, operators opting out set `Tmdb:Enabled` to `false` in the published `api-settings` JSON and verify the setting without printing credentials before rolling or restarting the API. T010 owns the migration note (FR-003/FR-004/FR-005; SC-008; `brief.md` step 2 / Q3/Q7).
- [ ] T006 [US1][US2] **Build lane**. After T004 and T005 both complete: Re-run focused suites only — `tests/LamuFlix.UnitTests/TmdbComposeTests.cs` and `scripts/home/Test-NewHomeCluster.ps1`; resolve in-scope failures until both green for the settings-file channel (FR-009; SC-003; `brief.md` step 2). Full .NET suite and property tests belong to T013, not here.

**Checkpoint**: Compose and home provisioning cut over; focused checks green.

---

## Phase 4: Deploy parity guard and gitleaks (US3)

**Goal**: Parity fixtures/check reject restored provider-key env wiring; obsolete allowlist removed.
**Independent Test**: Existing parity cases green; negative case fails; gitleaks clean.
**Order**: T007 → T008 → T009. Phase 4 completes before README (T010).

- [ ] T007 [US3] **Build lane**. After T006: Strip `Tmdb__Enabled` and `Tmdb__ApiKey` from `scripts/fixtures/deploy-parity/compose.yml`, `scripts/fixtures/deploy-parity/compose-extra-service/compose.yml`, and the four rendered manifests `pass/rendered.yaml`, `missing-service/rendered.yaml`, `compose-extra-service/rendered.yaml`, `digest-mismatch/rendered.yaml` without changing existing `expect.json` outcomes (FR-008; `brief.md` step 3 / Q5).
- [ ] T008 [US3] **Build lane**. After T007: In `scripts/Test-DeployParity.ps1`, assert parsed Compose API env and rendered Kubernetes API env reject `Tmdb__Enabled`, `Tmdb__ApiKey`, `Omdb__ApiKey`, and `Subtitles__OpenSubtitles__ApiKey`. Add negative fixture directory `scripts/fixtures/deploy-parity/provider-key-env/` (Compose input + `expect.json` + rendered input as required by `scripts/Test-DeployParityFixtures.ps1` convention) that fails when the env channel is restored. Base `provider-key-env` on passing inputs and exercise both API guards with inert values such as `REDACTED-FIXTURE`. Omit legacy `.env` assignment and interpolation syntax. Assert the specific provider-key rejection in `expect.json` using the existing output matcher as well as the failing exit. Confirm each guard can independently reject its own restored env entries using the same passing baseline. Preserve unrelated workload/port/digest/Ollama/OTEL checks (FR-008; SC-004; `brief.md` step 3 / Q5).
- [ ] T009 [US3] **Build lane**. After T002/T003/T008, check both rewritten tests and the negative fixture for the obsolete `TMDB_API_KEY=` parsing needle and unsafe fixture values. Remove its obsolete `.gitleaks.toml` allowlist entry. Run gitleaks against the final changed files with both rewritten tests and the fixture present. A verified need for a new exception goes to Patron (FR-011; SC-005; `brief.md` step 3 / Q6).

**Checkpoint**: Parity green with negative case; gitleaks allowlist cleaned. README may start only after this checkpoint.

---

## Phase 5: README and acceptance evidence (US4)

**Goal**: One configuration table plus cutover docs; automated checks green; both live receipts present or pending per deployment.
**Independent Test**: README review; evidence table commands all green; pending-per-deployment labels where a live receipt is missing — never alternate acceptance outcomes.
**Order**: T010 → T011 → T012. T010 after T009 (parity + gitleaks) per frozen brief.

- [ ] T010 [US4] **Build lane**. After T009 (parity and gitleaks complete per brief step 4 after step 3): Update `README.md` with one provider-key table (OMDb / TMDb / OpenSubtitles × `dotnet run` user secrets, Compose `docker/api.settings.json`, home `api-settings` Secret); default `Tmdb:Enabled: true`; opt-out via `Enabled: false`; explicit retention of any effective Compose override and any home `Tmdb:Enabled: false` override on each regeneration of published `api-settings`; enabled/blank-key retryable behaviour; and the cutover order — Compose: populate mounted JSON before recreate/roll, verify enrichment, then remove obsolete `.env` lines while retaining prior inputs until verification; home: publish updated `api-settings` before applying the new API deployment, verify rollout and enrichment, retain rollback inputs until verification succeeds, then post-verification manual `tmdb-credentials` cleanup in `lamuflix` — without echoing secret values. For fresh Compose setup, copy the tracked example to the gitignored settings path before startup. At cutover supply the TMDb key or preserve/set the intended opt-out before rolling. Keep keys stable through rollout and verify after it completes. After each publication, operators opting out set `Tmdb:Enabled` to `false` in the published `api-settings` JSON and verify the setting without printing credentials before rolling or restarting the API. Keep the one-time legacy Secret migration note in README (FR-006/FR-010; SC-007/SC-008; `brief.md` step 4 / Q3/Q7).
- [ ] T011 [US4] **Build lane**. After T008/T009/T010: Run `scripts/Test-DeployParityFixtures.ps1`, `scripts/Test-DeployParity.ps1`, `scripts/run-compose-smoke.ps1`, and `npm run e2e` from `web/`. Confirm `web/e2e/run.mjs` still generates `.runtime/compose.env` and `.runtime/api.settings.json`, preserves loopback providers and disabled LocalPlay, and passes the generated env file to Compose with the settings-file path (review-only — STOP for Patron ruling before editing it). Run Compose smoke and E2E smoke serially per worktree. In isolated Compose verification record actual startup behavior when the settings source is missing. Verify the actual E2E mount of generated `.runtime/api.settings.json` at `/app/appsettings.Production.json` with credential-redacted evidence. Record honest PASS/FAIL/`Could not run`. Every required hosted check must be green for this criterion to close; a failed or `Could not run` required check leaves the criterion unresolved and is never an alternate acceptance outcome. Carry this hosted-check evidence forward into T013 (FR-011/FR-012; SC-004/SC-005; `brief.md` step 4 / Q6/Q8).
- [ ] T012 [US4] **Build lane**. After T011: Capture both separate credential-redacted live Compose and home-cluster startup + OMDb+TMDb enrichment of a known title from mounted settings only, showing absence of runtime provider-key env wiring and absence of home API dependency on `tmdb-credentials`. Record rollback-input retention through verification in both receipts. Where the home opt-out applies, record redacted evidence of the post-publish setting check before rollout. Record relevant enabled/blank diagnostics if observed in scoped verification. Both receipts are closure criteria. If either receipt is unavailable, record that deployment's criterion as pending live acceptance — never fold stubbed smoke into live PASS and never treat pending as an alternate acceptance outcome. Carry receipt/pending status into T013 (FR-011; SC-006; `brief.md` Q6/Q8).

**Checkpoint**: Docs complete; automated evidence green or honestly unresolved; both live proofs present or pending per deployment.

---

## Phase 6: Polish — gates and Size M review

**Purpose**: Repo gates and three-axis review under Q8 closing bar; hosted-check and live-receipt evidence required at closure; constitution compliance before implementation merge.
**Checkpoint**: Honest gate labels; dispositions for accepted findings; T001b complete; no merge by seats.

- [ ] T013 **Build lane**. After T011 (hosted checks), T012 (both live receipts present or explicitly pending per deployment), and T001b (constitution amendment): Require T011 hosted-check evidence (deploy-parity fixtures + check, compose-smoke, e2e-smoke, gitleaks) green — a failed or `Could not run` required check leaves this gate unresolved. Require T012 both-live-receipt status (present or pending per deployment). Confirm T001b constitution amendment + Governance bump/Sync Impact Report are complete (VII no longer an open conflict on evidence). Run the **full** `dotnet test` suite as prescribed by the Closing bar (not a focused-suite substitute; focused checks remain at T006). Run property tests explicitly when tagged invariants apply; if the property-test gate has no tagged tests, Bernstein records the no-domain-invariants opt-out and the gate is `SKIP` (Q8 labels). Also run applicable Phase 3 gates against the actual diff — `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (and refactor `-Threshold 6` when applicable), `./scripts/run-jetbrains-inspectcode.ps1`, package audit, `dotnet format --verify-no-changes`, and mutation when production `src/` C# changed. Report exit-2 scope-empty as `SKIPPED (scope-empty)` (including mutation when no production C# and web gates when `web/` has no diff). `Could not run` and red required checks leave gates unresolved. Missing live receipts leave runtime acceptance pending per deployment and block implementation merge clearance (FR-011/FR-013; SC-005/SC-006; `brief.md` Q8/Q9).
- [ ] T014 **Sentry / Ledger / Compass**. After T013: Size M three-axis review under Q8 closing bar (Critical through Low); max two review/fix rounds; every accepted in-scope finding closes by verified fix or evidence-backed disposition on the PR; unresolved blockers → Bernstein; no silent waiver (`brief.md` Q8). **Constitution compliance (T001b) and both live receipts (T012) are required for implementation merge clearance.** See the brief for owner and closing-bar rules.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Owner Q9 checkbox approval + spec PR merge; blocks all Build work
- **Phase 1 (T001)**: Strictly read-only recon/analyze; blocks T001b and focused test rewrites. No constitution edit here
- **Phase 1b (T001b)**: Post-approval constitution amendment; before runtime cutover (T004); required before implementation merge
- **Phase 2**: Focused checks before production cutover (T002 ∥ T003; both before T004/T005)
- **Phase 3**: Template (T004) then template-consuming provisioning (T005); both before focused re-run (T006); after T001b
- **Phase 4**: After cutover so fixtures match reality; completes before README
- **Phase 5**: README after parity/gitleaks; then automated verification; then live receipts
- **Phase 6**: Gates/review after hosted-check, live-receipt, and constitution-compliance evidence recorded

### User Story Dependencies

- **US1 Compose (T002, T004, T006)**: Tests first, then Compose/example/env cutover (after T001b), then focused re-run
- **US2 Home (T003, T005, T006)**: Tests first; T005 after T004 template; both cutover tasks before T006
- **US3 Parity (T007–T009)**: After cutover green; before README
- **US4 Docs/evidence (T010–T012)**: T010 after T009; T011 after parity/gitleaks/README; T012 after automated smoke
- **Q9 constitution (T001b)**: After T000/T001; before T004; before implementation merge

### Parallel Opportunities

- T002 ∥ T003 after T001 (both must finish before T004/T005)
- T001b may proceed after T001 in parallel with T002/T003 (disjoint files) but **must** complete before T004
- T004 and T005 are **not** parallel for completion: T005 consumes the T004 template (sequence T004 → T005)
- T010 starts only after T009 (no early README completion ahead of parity/gitleaks)
- T012/T013/T014 are never `[P]` with incomplete prerequisites

---

## Implementation Strategy

### MVP First

1. T000 (owner checkbox approval + merge) → T001 (read-only)
2. T001b constitution amendment (Governance bump / Sync Impact Report)
3. Focused checks T002 ∥ T003
4. Atomic cutover T004 → T005 → T006 and validate focused checks independently

### Incremental Delivery

1. Add parity guard + gitleaks cleanup
2. README (after parity/gitleaks) + automated verification to green
3. Both live receipts (or pending per deployment) + gates carrying hosted-check evidence + Size M review
4. Confirm constitution compliance before implementation merge

### Notes

- Negative fixture directory name `provider-key-env` is Quill-named within ticket-owned deploy-parity fixtures (`brief.md` Q5/Q7).
- `web/e2e/run.mjs` and `appsettings.json` stay read-only unless Patron ratifies a ticket-forced regression edit.
- Provisioning must never delete `tmdb-credentials`.
- Principle VII is a **current conflict** until T001b lands; tasks do not declare VII PASS in this drafting round. No new or superseded ADR (Q9).
- See the brief for Gate 1 and delivery closing criteria.
