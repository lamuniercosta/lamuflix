# Feature Specification: Unify provider API keys into API settings

**Feature Branch**: `feature/565-spec`
**Created**: 2026-11-23
**Status**: gate1: provisional
**Input**: Ticket DEV-565 (parent DEV-100; size M); grill-closed `brief.md` (Q1–Q8) plus analyze-time `brief.md` Q9 / `CONCLUSIONS.md` Q9; Maestri notes `DEV-565`, `recon-DEV-565` (recon HEAD `15c1e43af7ba6c3d5f37efbabd4a313164588932`); `specs/DEV-565/ticket-text.txt`; `specs/PRODUCT.md`. No `ASSUMPTIONS.md` (no taste rulings). No new ADR and no ADR supersession (Quill does not author ADRs; Q9 cites none required).

## Owner question — Gate 1

Q9 supersedes Q8’s zero-checkbox status (`brief.md` Q9 owner checkbox; `CONCLUSIONS.md` Q9). Gate 1 stays closed until the owner approves the checkbox on the spec PR. Patron’s Q9 ruling defines the checkbox text; **Patron is not owner approval**. Quill does not invent PLAN FROZEN.

- [ ] **blocked: structural — Approve amending Constitution Principle VII** (`.specify/memory/constitution.md:673–674`) so Docker Compose and the Kubernetes home cluster receive API configuration and provider API keys via the mounted gitignored settings JSON (`docker/api.settings.json` mounted at `/app/appsettings.Production.json`) or mounted Kubernetes Secret (`api-settings`), while container environment variables supply non-API container initialization and host/container wiring, with no new ADR required?

## Clarifications

### Session 2026-11-23

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen Q1–Q8 runtime scope (unified settings channel for Compose and home cluster, TMDb enabled default with blank-key retryable behaviour, cutover and one-time Secret cleanup, OpenSubtitles example contract, deploy-parity guard, acceptance evidence, task order/file boundary, Size M review bar) is settled in `brief.md` Q1–Q8 and `CONCLUSIONS.md`. Analyze-time Q9 adds the open structural owner checkbox and a conditional constitution-only file-boundary addition; it leaves Q1–Q8 runtime and acceptance intact. Ticket Scope & Technical Design and acceptance criteria are already approved authority (`PRODUCT.md` §3). DEV-445 Q2 is superseded for Compose and home-cluster TMDb configuration only. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

Nothing visible: this ticket consolidates provider API key configuration into the mounted API settings file for Compose and the home cluster, updates provisioning, parity checks, focused tests, gitleaks allowlist cleanup, and README. The SPA, enrichment merge behaviour, provider adapters, and option validation are unchanged. Once keys are present in the mounted settings, titles enrich as they do today.

## Scope

**In scope** (`brief.md` Frozen scope and approach; Q1–Q8 runtime/acceptance; Q9 constitution-only addition; ticket Scope & Technical Design and acceptance criteria):

1. **Compose channel**: Mount gitignored `docker/api.settings.json` at `/app/appsettings.Production.json` remains the runtime source for `Omdb:ApiKey`, `Tmdb:Enabled` / `Tmdb:ApiKey`, and `Subtitles:OpenSubtitles:ApiKey`. Remove `Tmdb__Enabled` and `Tmdb__ApiKey` from the Compose `api` service environment and remove TMDb entries from `.env.example`. Preserve Postgres, RabbitMQ, library path, Ollama, and other existing non-provider settings.
2. **Home cluster channel**: Kubernetes reads the same key paths from the mounted `api-settings` Secret. Remove both TMDb environment entries and the `tmdb-credentials` Secret reference and example declaration. Home provisioning continues to read the three keys from the operator's external `secretSourcePath`, writes them into generated API settings JSON, and publishes that as `api-settings`. Retire `Get-TmdbKeyFile`, `tmdb.env`, and the separate Secret publish path.
3. **Defaults and opt-out**: Compose example and generated home settings default `Tmdb:Enabled` to `true`. Example uses empty `Tmdb:ApiKey`; home accepts a missing TMDb key. With TMDb enabled, a blank key makes an OMDb miss retryable. Operators opt out with `Tmdb:Enabled: false` in the mounted JSON. Home operators using that override apply it to the published settings when regenerating secrets. Single settings channel only.
4. **Settings example**: Extend placeholder-only `docker/api.settings.example.json` with `Tmdb` and full `Subtitles:OpenSubtitles` (`ApiKey: ""`, `BaseUrl: "https://api.opensubtitles.com"`, `UserAgent: "LamuFlix v1.0"`, `DownloadAllowedHosts: ["api.opensubtitles.com", "www.opensubtitles.com"]`, `MaxDownloadBytes: 1048576`, `RequestTimeout: "00:00:15"`). Preserve existing OMDb section and sibling `Subtitles:SaveEnabled` / `Subtitles:PreferredLanguages`.
5. **README**: One provider-key table with the three key paths and one location per run mode (`dotnet run` → user secrets; Compose → `docker/api.settings.json`; home cluster → mounted `api-settings` Secret). Document opt-out and enabled/blank-key behaviour; retention of any effective Compose override and any home `Tmdb:Enabled: false` override on each regeneration of published `api-settings`; and the publish-before-rollout / verify-before-cleanup / rollback-retention cutover order. Manual `tmdb-credentials` cleanup in namespace `lamuflix` is post-verification and outside provisioning.
6. **Parity and secret scan**: Strip legacy TMDb env from deploy-parity Compose/rendered fixtures; assert Compose and rendered API workloads reject `Tmdb__Enabled`, `Tmdb__ApiKey`, `Omdb__ApiKey`, and `Subtitles__OpenSubtitles__ApiKey`; add one targeted negative fixture case; remove obsolete `TMDB_API_KEY=` gitleaks test allowlist once unused.
7. **Verification**: Rewrite focused Compose and home provisioning tests; run deploy-parity fixtures and check, compose-smoke, e2e-smoke, and gitleaks to green (failed/`Could not run` = unresolved); capture both separate credential-redacted live Compose and home enrichment receipts (missing proof = pending per deployment, never an alternate acceptance outcome).
8. **Conditional constitution-only addition (Q9)**: After owner approval of the Gate 1 checkbox, amend `.specify/memory/constitution.md:673–674` solely for the scoped Principle VII wording (mounted Compose/home API settings channel; env vars for non-API initialization/wiring), plus Governance-required version bump and Sync Impact Report. No new ADR. This drafting round records the path only — it does not perform the amendment.

**Out of scope**:

- Changing local `dotnet run` (user secrets / gitignored `appsettings.Development.json` already unify all keys under Principle VII).
- Any change to provider adapters or option validation.
- Edits to `src/LamuFlix.Api/appsettings.json` or `web/e2e/run.mjs` except as read-only verification inputs unless a ticket-forced regression is identified and Patron ratifies an addition.
- New NuGet/npm dependency, project, top-level folder, architectural layer, database schema, public API shape, `Features:LocalPlay`, or `Process.Start`.
- New ADR or ADR supersession (Q9).
- Automated deletion of `tmdb-credentials` inside provisioning.
- Amending the constitution before owner checkbox approval, or treating Patron’s Q9 ruling as that approval.
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, VII PASS, or runtime acceptance from this draft.
- Anything else — follow-up issue, not a finding in this round (`brief.md` frozen scope).

**Precedence**: Ticket Scope & Technical Design and acceptance criteria are already approved authority (`PRODUCT.md` §3). Brief Q1–Q8 and `CONCLUSIONS.md` govern runtime/acceptance boundaries (DEV-445 Q2 superseded for Compose and home-cluster TMDb only). Q9 supersedes Q8’s owner-checkbox status and adds the constitution-only allowlist entry. A clean Keel `/speckit-analyze` advances to plan challenge and does not freeze the plan. Only Keel declares PLAN FROZEN after independent reread / challenge disposition.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only)*

**API settings channel**
The mounted JSON configuration that carries provider keys at runtime: Compose uses gitignored `docker/api.settings.json` mounted to `/app/appsettings.Production.json`; the home cluster uses the `api-settings` Secret mounted the same way.
_Avoid_: reading provider keys from Compose `.env`, dedicated API env vars, or a second Kubernetes Secret at runtime

**Operational secret ingestion**
Home provisioning only: the operator's external `secretSourcePath` file supplies `OMDB_API_KEY`, `TMDB_API_KEY`, and `OPENSUBTITLES_API_KEY` so scripts can render them into generated settings JSON before publish. It is not a runtime pod configuration channel.
_Avoid_: treating `secretSourcePath` as a pod env source; publishing a second provider Secret

**Enabled blank-key behaviour**
`Tmdb:Enabled: true` with a blank `Tmdb:ApiKey` preserves the DEV-445 rule that an OMDb miss becomes retryable until a key is supplied or TMDb is opted out with `Enabled: false`.
_Avoid_: treating enabled+blank as disabled-chain equivalence; requiring a TMDb key solely to render home `api-settings`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compose single settings channel (Priority: P1)

An operator configures OMDb, TMDb, and OpenSubtitles in `docker/api.settings.json`. Compose no longer wires TMDb through `.env` / API environment variables. The tracked example documents every provider section with placeholders only.

**Why this priority**: Ticket acceptance requires no provider key from `.env` or dedicated env var in Compose, and a complete placeholder example including OpenSubtitles.

**Independent Test**: `TmdbComposeTests` asserts the JSON mount, absence of TMDb Compose/env wiring, placeholder-only provider sections with exact OpenSubtitles fields, and disabled API base default with blank key — without reading real secrets.

**Acceptance Scenarios**:

1. **Given** `docker-compose.yml` API service configuration, **When** inspected, **Then** it mounts `docker/api.settings.json` to `/app/appsettings.Production.json` and does not define `Tmdb__Enabled` or `Tmdb__ApiKey`.
2. **Given** `.env.example`, **When** inspected, **Then** it has no TMDb entries while still documenting non-provider Compose values as today.
3. **Given** `docker/api.settings.example.json`, **When** inspected, **Then** it contains placeholder-only `Omdb`, `Tmdb` (`Enabled: true`, `ApiKey: ""`), and `Subtitles:OpenSubtitles` with the exact Q4 field set, preserving sibling subtitle settings.
4. **Given** `src/LamuFlix.Api/appsettings.json` (verification input), **When** inspected, **Then** `Tmdb:Enabled` remains `false` with blank key.

---

### User Story 2 - Home cluster single Secret channel (Priority: P1)

An operator provisions the home cluster so all provider keys live in the published `api-settings` Secret. The dedicated TMDb Secret path is retired from manifests and provisioning; cleanup of an existing Secret is a documented one-time operator step after verification.

**Why this priority**: Ticket acceptance requires the home cluster to enrich using only `api-settings`, with a migration note for the existing TMDb Secret.

**Independent Test**: Stubbed `Test-NewHomeCluster.ps1` verifies generated OMDb/TMDb/OpenSubtitles settings with TMDb key present and absent, the mounted Secret path, and absence of `tmdb.env` and of a second published Secret.

**Acceptance Scenarios**:

1. **Given** `deploy/k8s/base/api-deployment.yaml` and `deploy/home/secrets.example.json`, **When** inspected, **Then** they no longer reference `Tmdb__Enabled` / `Tmdb__ApiKey` or declare `tmdb-credentials`, and the API still mounts `api-settings`.
2. **Given** `secretSourcePath` with OMDb, OpenSubtitles, and optional TMDb keys, **When** `New-HomeCluster.ps1` runs, **Then** generated API settings contain those provider sections (TMDb enabled by template default; missing TMDb key allowed) and only `api-settings` is published for API settings.
3. **Given** provisioning after this change, **When** scripts run, **Then** they do not create `tmdb.env`, do not call `Get-TmdbKeyFile`, and do not delete `tmdb-credentials`.
4. **Given** an existing cluster that still has `tmdb-credentials`, **When** the operator follows the migration note, **Then** cleanup is a manual post-verification step in namespace `lamuflix` after enrichment is confirmed, with prior inputs retained until then for rollback.

---

### User Story 3 - Deploy parity rejects restored env channel (Priority: P1)

Deploy-parity fixtures and checks stay aligned with the unified channel and fail when legacy provider-key env wiring returns.

**Why this priority**: Ticket requires Compose/k8s parity and a guard that no provider API key is read from dedicated env vars.

**Independent Test**: `Test-DeployParityFixtures.ps1` and `Test-DeployParity.ps1` stay green for existing cases; the new negative case fails when the env channel is restored; unrelated parity checks remain intact.

**Acceptance Scenarios**:

1. **Given** deploy-parity shared/override Compose fixtures and the four existing rendered manifests, **When** updated, **Then** they no longer carry `Tmdb__Enabled` / `Tmdb__ApiKey`, and existing `expect.json` outcomes are preserved.
2. **Given** `Test-DeployParity.ps1`, **When** it parses Compose API env and rendered Kubernetes API env, **Then** it rejects `Tmdb__Enabled`, `Tmdb__ApiKey`, `Omdb__ApiKey`, and `Subtitles__OpenSubtitles__ApiKey`.
3. **Given** one new negative fixture directory under `scripts/fixtures/deploy-parity/` following the existing runner convention, **When** the fixture runner executes that case, **Then** the parity check fails for the restored env channel.

---

### User Story 4 - Operator docs and acceptance evidence (Priority: P2)

README presents one configuration table and the cutover/rollback sequence. Required automated checks must be green; both separate live Compose and home enrichment receipts are closure criteria. A failed or `Could not run` required check remains unresolved; missing live evidence is pending per deployment — never an alternate acceptance outcome.

**Why this priority**: Ticket acceptance requires the single table, green hosted checks, and real deployment enrichment proof.

**Independent Test**: README review; automated suite listed in brief evidence table (all required checks green); credential-redacted live receipts for both deployments, or pending-per-deployment labels when a receipt is missing.

**Acceptance Scenarios**:

1. **Given** README, **When** read, **Then** it has one provider-key table covering the three key paths and one location per run mode; documents default `Tmdb:Enabled: true`, opt-out via `Enabled: false`, and that operators must retain any effective Compose override and any home `Tmdb:Enabled: false` override on each regeneration of published `api-settings`; documents enabled/blank-key behaviour; and spells out Compose and home cutover order — publish/populate mounted settings before rollout, verify enrichment before cleanup, and retain prior inputs for rollback until verification succeeds — including post-verification manual `tmdb-credentials` cleanup in `lamuflix`.
2. **Given** focused tests, deploy-parity, compose-smoke, e2e-smoke, and gitleaks, **When** run after the cutover, **Then** each required automated check is green. A failed or `Could not run` required check leaves that criterion unresolved and is never folded into PASS or treated as an alternate acceptance outcome.
3. **Given** live Compose and home cluster rollouts, **When** enrichment of a known title is verified, **Then** credential-redacted evidence for each deployment shows OMDb + TMDb enrichment from mounted settings only, with no runtime provider-key env wiring and no home API dependency on `tmdb-credentials`. Both separate live receipts are closure criteria; missing proof for a deployment is recorded as pending live acceptance for that deployment only — never as PASS via stubbed smoke or as an alternate acceptance outcome.
4. **Given** `web/e2e/run.mjs` as a verification input, **When** e2e-smoke runs, **Then** it continues to generate `.runtime/compose.env` for Compose inputs and `.runtime/api.settings.json` for API settings; any future TMDb setting for E2E belongs in the generated JSON. The runner stays review-only unless verification exposes a ticket-forced settings-channel defect ratified by Patron.

### Edge Cases

- TMDb enabled with blank key → OMDb miss is retryable (documented; not disabled-chain equivalence).
- Home provisioning with missing `TMDB_API_KEY` → settings still generate with template `Enabled: true` and blank/absent key; no second Secret.
- Operator opt-out → `Tmdb:Enabled: false` in mounted JSON; home operators re-apply that override when regenerating published settings.
- Existing `tmdb-credentials` Secret → retained until post-verification manual cleanup; provisioning never deletes it.
- Restored provider-key env in Compose or rendered API → deploy-parity fails (negative fixture).
- Live enrichment proof unavailable at verification time → that deployment's criterion is pending live acceptance; never stubbed-smoke PASS and never an alternate acceptance outcome.
- Required automated check failed or `Could not run` → criterion unresolved; never PASS.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Compose MUST supply provider keys only via mounted `docker/api.settings.json` (`Omdb:ApiKey`, `Tmdb:Enabled`/`Tmdb:ApiKey`, `Subtitles:OpenSubtitles:ApiKey`) and MUST NOT define `Tmdb__Enabled` or `Tmdb__ApiKey` on the API service (`brief.md` Q1).
- **FR-002**: `.env.example` MUST NOT document TMDb entries; non-provider Compose values remain as today (`brief.md` Q1).
- **FR-003**: Home cluster API MUST read provider keys only from mounted `api-settings`; manifests and examples MUST NOT reference `tmdb-credentials` or TMDb env `secretKeyRef` wiring (`brief.md` Q1).
- **FR-004**: Home provisioning MUST ingest `OMDB_API_KEY`, `TMDB_API_KEY`, and `OPENSUBTITLES_API_KEY` from external `secretSourcePath`, render them into generated API settings JSON, publish `api-settings`, and MUST NOT create `tmdb.env`, call `Get-TmdbKeyFile`, publish a second provider Secret, or delete `tmdb-credentials` (`brief.md` Q1/Q3/Q7).
- **FR-005**: `docker/api.settings.example.json` and home-generated settings MUST default `Tmdb:Enabled` to `true`; the example MUST use `Tmdb:ApiKey: ""`; home MUST accept a missing TMDb key; opt-out is `Tmdb:Enabled: false` in the mounted JSON only (`brief.md` Q2/Q7).
- **FR-006**: Enabled TMDb with blank key MUST retain documented retryable OMDb-miss behaviour; README MUST explain the opt-out (`brief.md` Q2).
- **FR-007**: `docker/api.settings.example.json` MUST include placeholder-only `Subtitles:OpenSubtitles` with exact Q4 values and MUST preserve OMDb plus sibling Subtitles keys; adapters/validation unchanged (`brief.md` Q4).
- **FR-008**: Deploy-parity fixtures MUST drop legacy TMDb env data without changing existing case expectations; `Test-DeployParity.ps1` MUST reject the four legacy provider-key env names on Compose and rendered API; one negative fixture MUST fail when that channel is restored (`brief.md` Q5).
- **FR-009**: Focused tests MUST cover Compose mount/channel/example contracts and home present/absent TMDb key provisioning without second Secret (`brief.md` Q6/Q7).
- **FR-010**: README MUST provide one provider-key table (three keys × three run modes); MUST document retaining any effective Compose override and any home `Tmdb:Enabled: false` override on each regeneration of published `api-settings`; MUST spell out publish-before-rollout, verify-before-cleanup, and rollback-input retention until verification; and MUST include post-verification Secret cleanup guidance without echoing secrets (`brief.md` Q1/Q3/Q7).
- **FR-011**: Acceptance closure MUST require green deploy-parity, compose-smoke, e2e-smoke, and gitleaks (after obsolete allowlist removal), plus both separate credential-redacted live Compose and home enrichment receipts. A failed or `Could not run` required check leaves that criterion unresolved. Missing live evidence is pending per deployment. Neither unresolved checks nor missing receipts are alternate acceptance outcomes (`brief.md` Q6/Q8).
- **FR-012**: Tracked file edits MUST stay within the Q1/Q5/Q7 allowlist, plus `.specify/memory/constitution.md` solely for the owner-approved Q9 Principle VII amendment and its Governance version bump / Sync Impact Report; `appsettings.json` and `web/e2e/run.mjs` are read-only verification inputs unless Patron ratifies a ticket-forced addition (`brief.md` Q7/Q9).
- **FR-013**: Size M closing bar MUST require Risk/Standards/Spec reports, at most two review rounds, disposition of every accepted in-scope finding Critical through Low, the full .NET suite as prescribed, explicit property-test run or Bernstein `SKIP` when no tagged invariants, honest gate labels including `SKIPPED (scope-empty)`, hosted-check evidence carried into gate tasks, unresolved `Could not run` / red required checks / missing live proof, and constitution compliance (post-approval Q9 amendment path complete) before the implementation merge (`brief.md` Q8/Q9).
- **FR-014**: Gate 1 MUST remain closed until the owner approves the Q9 structural checkbox on the spec PR; T000 MUST verify that owner approval and the spec PR merge before Build work. Patron’s Q9 ruling is not owner approval (`brief.md` Q9).

### Key Entities

- **API settings document**: JSON carrying provider sections (`Omdb`, `Tmdb`, `Subtitles` including `OpenSubtitles`) plus non-provider API configuration.
- **Compose env channel**: Non-provider container values only after this change (Postgres, RabbitMQ, library path, and other existing non-API-provider settings).
- **Home `api-settings` Secret**: Single published Kubernetes Secret for mounted API settings.
- **Operational `secretSourcePath`**: External provisioning input for the three provider key env names — not runtime pod config.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No provider API key is read from `.env` or from a dedicated env var in Compose or the cluster; each provider key has exactly one documented location per run mode.
- **SC-002**: `docker/api.settings.example.json` documents every provider section, including `Subtitles:OpenSubtitles`, with placeholders only and the ratified field set.
- **SC-003**: Focused Compose and home provisioning tests pass for the settings-file channel, including TMDb key present and absent for home.
- **SC-004**: Deploy-parity fixtures and check pass; the negative restored-env case fails as expected; unrelated parity checks remain green.
- **SC-005**: deploy-parity, compose-smoke, e2e-smoke, and gitleaks are green. A failed or `Could not run` required check leaves the criterion unresolved and is never silent PASS or an alternate acceptance outcome.
- **SC-006**: Both separate credential-redacted live Compose and home receipts show OMDb + TMDb enrichment of a known title from mounted settings only. Both receipts are closure criteria; missing proof is pending per deployment, never an alternate acceptance outcome.
- **SC-007**: README contains the single key-configuration table; documents override retention on settings regeneration and the publish-before-rollout / verify-before-cleanup / rollback-retention order; `.env.example` no longer mentions TMDb.
- **SC-008**: Provisioning never deletes `tmdb-credentials`; migration docs place cleanup after verification.

## Assumptions

- No taste assumptions for this ticket; no `ASSUMPTIONS.md` entries.
- Grill Q1–Q8 fully decide channel, defaults, cutover, OpenSubtitles example, parity guard, evidence, runtime file boundary, and closing bar; Q9 decides the open owner checkbox and conditional constitution-only addition — Quill invents no further behaviour.
- Negative deploy-parity fixture directory name is Quill-named within the ticket-authorized `scripts/fixtures/deploy-parity/` area (`provider-key-env`), following the existing runner convention (`brief.md` Q5/Q7).
- Phase A produces Spec Kit documents only; clean analyze leads to plan challenge (not freeze); Gate 1 stays closed until Q9 owner checkbox approval; PLAN FROZEN, implementation, constitution amendment execution, and runtime acceptance remain later steps. Principle VII remains a current conflict until the owner-approved amendment lands — not declared PASS in this draft (`brief.md` Q9; `plan.md` Constitution Check).
