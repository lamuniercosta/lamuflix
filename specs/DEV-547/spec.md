# Feature Specification: First release v0.1.0 (CHANGELOG and running-app version)

**Feature Branch**: `feature/547-spec`

**Created**: 2026-11-24

**Status** — Plan frozen after Phase 2 step 6. Gate 1 and live release acceptance remain pending.

**Input**: Ticket DEV-547 (parent DEV-492; size M); grill-closed `brief.md` (Q1–Q6); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-547`, `recon-DEV-547`; `specs/PRODUCT.md`. Quill invents no decisions beyond the brief.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Frozen scope / Grill questions; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Live tag push, promotion readback, and Rigger’s YouTrack evidence remain Phase B / post-merge acceptance work.

## Clarifications

### Session 2026-11-24

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: API surface (`GET /api/version`, `{ release, commit }`, HTTP 200), metadata names and normalization (`LAMUFLIX_COMMIT` / `LAMUFLIX_RELEASE`, full SHA, `unknown`, JSON `null`), single UI placement rule and display format, pre-merge verification matrix, release sequencing with DEV-546 first, review closing bar, and frozen exclusions are settled in `brief.md` Q1–Q6 and `CONCLUSIONS.md`. Presentation items tagged `[assumed]` in `ASSUMPTIONS.md` are Patron-ratified. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour. Any gap Quill cannot close from brief/CONCLUSIONS goes to `DRAFTING_RECEIPT.md` as `needs decision:`.

## What the User Will See *(mandatory)*

- **Where**: Exactly one place in the running web app — the DEV-560 status page if that page has landed by Phase B pickup, otherwise a footer line in the app shell (`App.tsx` per brief Q3).
- **What the user sees or does**: A single small version line of the form `<release|dev> (<first 8 SHA characters|unknown>)`, for example `v0.1.0 (8a2deb9b)` or `dev (8a2deb9b)`. While the version request is loading or if it fails, the line is omitted (no placeholder or invented values).
- **What it needs to show up**: Default Compose stack with the version endpoint available; Compose supplies commit metadata through the Q2 contract and leaves release empty (so the usual local line is `dev (…)`). After the authorized `v0.1.0` promotion, the home app at port 3100 shows `v0.1.0` and the matching short commit.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: Two Compose success cases against the Q3-selected location (same cross-surface uniqueness rule): **(a) configured short SHA** — **Given** the default Compose stack with the version endpoint returning a successful response where `release` is JSON `null` and `commit` is a known full SHA, **When** the viewer opens the app, **Then** exactly one version line appears as `dev (` + first eight commit characters + `)` in the Q3-selected location, and the other candidate surface has none — if the status page is selected, the app shell footer has no version line; if the `App.tsx` footer is selected, any landed DEV-560 status page has no version line (when that page is absent, uniqueness is within the shell only); **(b) unconfigured unknown** — **Given** the default Compose stack with the version endpoint returning a successful response where `release` is JSON `null` and `commit` is `"unknown"`, **When** the viewer opens the app, **Then** exactly one version line appears as `dev (unknown)` in the Q3-selected location, with the same cross-surface uniqueness rule as (a).
- **SMOKE-002**: Two separate Compose checks against the Q3-selected location (and the same cross-surface uniqueness rule as SMOKE-001 when both surfaces exist), each with a deterministic hold of the browser version request until the selected UI is observed, then a controlled release or controlled failure with bounded cleanup: **(a) in-flight** — **Given** the version request is held open, **When** the viewer observes the selected UI location during the hold, **Then** no version line and no placeholder version text is shown; after a successful release, the expected settled line appears; **(b) forced failure** — **Given** the held request is released as a controlled failure, **When** the selected UI location settles, **Then** no version line and no placeholder version text is shown. Record the interception/hold method, absence during the hold, and the settled result.

## Scope

**In scope** (`brief.md` Frozen scope / Approach and file scope / Task ordering; Q1–Q6; ticket Wanted 2–6 and Done when at `recon-DEV-547:54–69`):

1. **Release documentation**: Root `CHANGELOG.md` in Keep a Changelog format with `Unreleased` and `v0.1.0`. Written for the home-cluster owner from merged behaviour. The first entry covers library browse and filters, movie details, watchlist, OMDb and TMDb enrichment, recommendations, in-browser playback, subtitles, LocalPlay, and home deployment. Add a one-line README link to it.
2. **Build and release metadata**: Bake the exact source commit into API and web images via CI (`container-build` and `publish-images`) and both Dockerfiles (build args → final image `ENV` only). Carry the release tag into running home pods at promote time beside existing digest pins using the same safely serialized value written to `RELEASE.json`; manual promotion leaves release empty. Compose and promotion omit runtime `LAMUFLIX_COMMIT`, preserving the baked image value. A separately operator-supplied runtime `LAMUFLIX_COMMIT` becomes the effective process value (full non-whitespace passes through; empty/whitespace → `"unknown"`). No second variable (Patron P2.6-2).
3. **Version API**: Dedicated `GET /api/version` under the existing `/api` group returns HTTP `200 OK` with `{ release, commit }` (`release` nullable string, `commit` string). Full configured commit; `unknown` when commit metadata is unset/empty/whitespace; unset/empty/whitespace release serializes as JSON `null`. Contract chain C# DTO → OpenAPI → generated TypeScript.
4. **Running-app display**: Consume the generated contract in exactly one UI location per Q3 pickup rule; display format and loading/failure omission per Q3 / `ASSUMPTIONS.md`.
5. **Pre-merge verification**: API integration and normalization coverage; OpenAPI generation and drift check; web state tests (tagged, `dev`, loading, failure, one-location); CI build-arg and tagged/manual promotion rendering checks; offline `scripts/Test-DeployParity.ps1` / `deploy-parity` green; configured C# and web gates with honest SKIPPED/N/A labels.
6. **First release acceptance (post-merge)**: After DEV-546 and DEV-547 are on `main` and the nightly gate passes on that exact commit, the authorized release operator pushes `v0.1.0`. Read back green tag-triggered `promote-home`, GitHub Release, `RELEASE.json` on `env/home` with `"release": "v0.1.0"`, and port 3100 showing `v0.1.0` plus the short commit. Rigger posts the tag-run link and version-line screenshot to DEV-547 before closure.

**Out of scope**:

- Tagging images `v0.1.0` in GHCR.
- Release notes for other users; install or upgrade guides for other machines.
- Supply-chain checks (DEV-558).
- Automatic version bumps or release tooling.
- Any change to the gate (DEV-546 owns the lookup).
- New NuGet/npm dependency; new project/top-level folder/architectural layer; database schema; `Features:LocalPlay`, secrets hardcoding, or `Process.Start`.
- Anything else is a follow-up issue, not a finding in this round (charter threshold before filing).

**Precedence**: Ticket Wanted / Done when / Where it lands (`recon-DEV-547`) are approved authority (`PRODUCT.md` §3). Brief Q1–Q6 and `CONCLUSIONS.md` govern plan boundaries. Keel recorded the freeze after an independent reread. See `CONCLUSIONS.md` for the dispositions.

**Dependencies**: DEV-546 required first. DEV-560 optional (display location only).

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**Release metadata (`release`)**
Nullable string naming the release tag that reached the running pods (for example `v0.1.0`); JSON `null` when unset, empty, or whitespace, including untagged builds and manual promotions.
_Avoid_: inventing a release string for local/Compose empty release; treating image tags in GHCR as the release record

**Commit metadata (`commit`)**
Full source SHA baked into the image via build args / final image `ENV`. Compose and promotion omit runtime `LAMUFLIX_COMMIT` so the baked value is preserved. A separately operator-supplied runtime value is the effective process value (full non-whitespace passes through; empty/whitespace → `"unknown"`). `"unknown"` also applies when unset/empty/whitespace in an unconfigured local build. No second variable (P2.6-2).
_Avoid_: reporting a fabricated SHA; shortening in the API response (short form is display-only); Compose/promotion runtime shadowing of the baked commit

**Version line**
Single UI string `<release|dev> (<first-8-SHA-chars|unknown>)` in one pickup-selected location; omitted while loading or on request failure. Render `"unknown"` verbatim; shorten a full SHA only in the UI. Release text is opaque.
_Avoid_: second copy of the line; placeholder copy; hand-rolled response types; truncating `"unknown"`

Consumed vocabulary (already in use; not redefined): Keep a Changelog, `RELEASE.json`, promote-home, contract chain C# DTO → OpenAPI → TypeScript, home deployment, LocalPlay.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Version endpoint reports release and commit (Priority: P1) 🎯 MVP

Operators and the web app can read `{ release, commit }` from a dedicated version endpoint that normalizes empty metadata the same way in every environment.

**Why this priority**: Without a stable, tested contract, UI display and release readback cannot agree on what is running.

**Independent Test**: Isolated per-case `WebApplicationFactory` coverage (nonparallel collection; finally restore) proves HTTP 200, configured full commit, explicit JSON `null` when release is absent, a configured release tag, empty/whitespace normalization, `"unknown"` for unconfigured local commit metadata, and P2.6-2 override cases. OpenAPI export/Verify and generated TypeScript/MSW expose the shape via the P2.6-1 envelope.

**Acceptance Scenarios**:

1. **Given** configured commit metadata and no release metadata, **When** `GET /api/version` is called, **Then** the response is HTTP 200 with the full commit string and `"release": null`.
2. **Given** configured commit and release tag metadata, **When** `GET /api/version` is called, **Then** the response is HTTP 200 with both strings as configured.
3. **Given** unset, empty, or whitespace commit metadata, **When** `GET /api/version` is called, **Then** `commit` is `"unknown"`.
4. **Given** unset, empty, or whitespace release metadata, **When** `GET /api/version` is called, **Then** `release` is JSON `null`.

---

### User Story 2 - Commit and release metadata reach images and pods (Priority: P1)

CI bakes the exact source commit into both images; home promotion injects the release tag into both pods beside digest pins; Compose keeps parity with empty release.

**Why this priority**: The endpoint can only report what the delivery path actually carries.

**Independent Test**: Verify both CI Docker jobs pass the exact source SHA as build-arg input to both Dockerfiles (final image ENV only); verify tagged vs manual (empty) promotion rendering with no runtime `LAMUFLIX_COMMIT`; verify P2.6-2 override evidence; offline `scripts/Test-DeployParity.ps1` actual verdict stays green.

**Acceptance Scenarios**:

1. **Given** `container-build` and `publish-images`, **When** images are built, **Then** both Dockerfiles receive the exact source SHA as build-arg `LAMUFLIX_COMMIT` and persist it as final-image `ENV` (no other commit configuration path in this ticket).
2. **Given** a tagged home promotion, **When** manifests are rendered, **Then** both api and web pods receive safely serialized `LAMUFLIX_RELEASE` from the same `RELEASE_TAG` used for `RELEASE.json`, with no runtime `LAMUFLIX_COMMIT` assignment (baked commit preserved).
3. **Given** a manual promotion, **When** manifests are rendered, **Then** release is empty (`release: null` in `RELEASE.json` terms) while digests still update, still with no runtime `LAMUFLIX_COMMIT`.
4. **Given** Compose, **When** parity is checked offline, **Then** commit is supplied only via build args (no runtime `LAMUFLIX_COMMIT`), release is empty, and `deploy-parity` stays green.
5. **Given** an operator-supplied runtime `LAMUFLIX_COMMIT` outside Compose/promotion configs, **When** the process reads metadata, **Then** a nonblank value passes through in full and an empty/whitespace value yields `"unknown"` (P2.6-2).

---

### User Story 3 - App shows one version line (Priority: P1)

The running app shows release and short commit in exactly one place, using the generated version contract.

**Why this priority**: Done when requires the owner to see `v0.1.0` and the commit on port 3100 after release; local Compose must show the `dev` form without blocking the shell.

**Independent Test**: Generated-MSW web state tests cover tagged display, successful null-release `dev` with short commit, successful null-release verbatim `dev (unknown)`, deterministic in-flight hold omission, controlled failed-response omission, and one-location uniqueness across shell and status page when both exist after Q3 pickup selection (with Phase 6 drift recheck).

**Acceptance Scenarios**:

1. **Given** a successful version response with release `v0.1.0` and a full SHA, **When** the selected UI location renders, **Then** the line is `v0.1.0 (` + first eight SHA characters + `)` (SHA shortened only in the UI; release rendered as opaque text).
2. **Given** a successful version response with `release` null and a full SHA commit, **When** the selected UI location renders, **Then** the line is `dev (` + first eight SHA characters + `)`.
3. **Given** a successful version response with `release` null and `commit` `"unknown"`, **When** the selected UI location renders, **Then** the line is exactly `dev (unknown)` (`"unknown"` rendered verbatim, not truncated).
4. **Given** a version request held open with a deterministic interception, **When** the selected UI location is observed during the hold, **Then** the version line is absent (no placeholder); after a successful controlled release, the expected settled line appears.
5. **Given** a version request released as a controlled failure, **When** the selected UI location settles, **Then** the version line is absent (no placeholder).
6. **Given** Phase B pickup, **When** display location is chosen, **Then** the Q3 rule selects the DEV-560 status page if that page has landed, otherwise the `App.tsx` footer, and exactly that one location shows the version line. When both candidate surfaces are present, uniqueness is checked across shell and status page (never both show the line); when the status page is absent, uniqueness is within the selected shell only. Later DEV-560 landing after pickup is reported through the drift path before any placement change.

---

### User Story 4 - Owner-facing CHANGELOG and README link (Priority: P2)

The repository records what runs on the home cluster for `v0.1.0` and points to it from the README.

**Why this priority**: Documentation can advance in parallel with implementation; acceptance still requires the file and link before closure.

**Independent Test**: `CHANGELOG.md` exists in Keep a Changelog form with `Unreleased` and `v0.1.0` covering the Wanted 2 topic list from merged behaviour; README has one new line linking to it.

**Acceptance Scenarios**:

1. **Given** the delivery branch with changelog/README authored in parallel from the merged source for the frozen topic list, **When** the entry is checked before pre-merge gates, **Then** it uses Keep a Changelog format with `Unreleased` and a `v0.1.0` entry that matches merged behaviour (correct any drift before gates).
2. **Given** the README, **When** the delivery diff is reviewed, **Then** exactly one new line links to `CHANGELOG.md`.

---

### User Story 5 - First `v0.1.0` tag and deployment readback (Priority: P2)

After both tickets land and the nightly gate passes on that commit, the authorized operator ships `v0.1.0` and evidence is posted to the ticket.

**Why this priority**: Closure criteria are post-merge; they must not block pre-merge delivery verification, but they remain in scope for ticket Done when.

**Independent Test**: Eligible exact-SHA nightly; pre-push SHA/tag-state checks with halt/escalate on mismatch; tag run green; GitHub Release exists; `RELEASE.json` records safely serialized `"release": "v0.1.0"` plus target commit and digests; live port 3100 shows `v0.1.0` and short commit (harness blockers distinguished); Rigger comment carries tag-run link and screenshot.

**Acceptance Scenarios**:

1. **Given** DEV-546 and DEV-547 on `main`, **When** the initially considered head has no qualifying green nightly, **Then** wait for an eligible `main` commit that still contains both tickets and has existing green nightly evidence on that exact SHA; the Conductor identifies the authorized release operator.
2. **Given** that eligible SHA, **When** the authorized release operator prepares to push `v0.1.0`, **Then** the operator checks the recorded nightly SHA, the local proposed tag target, and remote tag state before pushing; if the tag is already bound to the wrong SHA or a mismatch is found after pushing, halt and escalate before further release action.
3. **Given** a successful tag push on the eligible SHA, **When** promotion completes, **Then** the tag-triggered `promote-home` run is green, a GitHub Release exists, and `RELEASE.json` on `env/home` has `"release": "v0.1.0"` with that target commit and deployed image digests (same safely serialized release value as the manifests).
4. **Given** the deployed home app, **When** port 3100 is opened, **Then** the version line shows `v0.1.0` and the matching short commit; distinguish an unavailable port-forward or readback harness from app behaviour — a harness blocker is recorded separately, but the required live port-3100 receipt still blocks ticket closure.
5. **Given** the above evidence, **When** Rigger comments on DEV-547, **Then** the comment includes the tag-run link and the version-line screenshot before closure.

---

### Edge Cases

- Unset, empty, or whitespace effective `LAMUFLIX_COMMIT` → API `commit` is `"unknown"`; UI shows `unknown` verbatim inside the parentheses when the request succeeds with that value.
- Unset, empty, or whitespace `LAMUFLIX_RELEASE` → API `release` is JSON `null`; UI shows `dev`; set release is rendered as opaque text.
- Absent runtime `LAMUFLIX_COMMIT` (Compose/promotion omit it) → baked image `ENV` is preserved (P2.6-2).
- Explicit operator-supplied nonblank runtime `LAMUFLIX_COMMIT` → that full value is the effective process commit (P2.6-2).
- Explicit operator-supplied empty/whitespace runtime `LAMUFLIX_COMMIT` → API `commit` is `"unknown"` (P2.6-2).
- In-flight version request (deterministic hold) → no version line and no placeholder during the hold; successful release shows the settled line (separate from failure).
- Failed version request (controlled failure after hold) → no version line and no placeholder (separate from in-flight).
- Successful `release` null with `commit` `"unknown"` → UI shows `dev (unknown)` (`"unknown"` not truncated).
- Manual promotion → empty release in manifests / `RELEASE.json` `release: null`, no runtime `LAMUFLIX_COMMIT`, baked commit preserved.
- DEV-560 not landed at Phase B pickup → footer in `App.tsx` only; if landed, status page only; later landing after pickup → drift path before placement change.
- Compose parity must remain green with release empty and commit supplied only via build args (no runtime `LAMUFLIX_COMMIT`).
- OpenAPI/Verify regen that introduces unrelated drift vs pickup HEAD → stop before client generation; resolve through the reviewed generation flow.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose `GET /api/version` under the existing `/api` group returning HTTP `200 OK` with body `{ release, commit }` where `release` is a nullable string and `commit` is a string (`brief.md` Q1).
- **FR-002**: System MUST report the full effective process commit string, and MUST use `"unknown"` when commit metadata is unset, empty, or whitespace (`brief.md` Q2; P2.6-2). Capture and normalize process metadata once per host when mapping the route (brief decision 1).
- **FR-003**: System MUST serialize unset, empty, or whitespace release metadata as JSON `null` (`brief.md` Q2). The same safely serialized release value MUST be used for manifests and `RELEASE.json`; the UI renders release as opaque text (brief decision 4).
- **FR-004**: CI jobs `container-build` and `publish-images` MUST pass the exact full source SHA as build arg `LAMUFLIX_COMMIT` to both `docker/Dockerfile.api` and `docker/Dockerfile.web`, and both Dockerfiles MUST persist it as final-image `ENV`. DEV-547 configures commit only via those build args and final image ENV — no second variable (`brief.md` Q2; Wanted 3; P2.6-2).
- **FR-005**: Home promotion MUST inject `LAMUFLIX_RELEASE` into api and web pod manifests from the same `RELEASE_TAG` used for `RELEASE.json`, empty on manual promotion, and MUST omit any runtime `LAMUFLIX_COMMIT` assignment so the image-baked commit is preserved (`brief.md` Q2; Wanted 4; P2.6-2).
- **FR-006**: Compose MUST supply commit only via build args (omit runtime `LAMUFLIX_COMMIT`) and empty release, keeping `scripts/Test-DeployParity.ps1` green. A separately operator-supplied runtime `LAMUFLIX_COMMIT` is the effective process value (full non-whitespace passes through; empty/whitespace → `"unknown"`) (`brief.md` Q2; Done when 3; P2.6-2).
- **FR-007**: Web MUST consume the generated OpenAPI TypeScript type for `GET /api/version` with no hand-rolled duplicate. Generator-only updates are authorized solely for `web/src/api/openapi.json`, `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` via the P2.6-1 export/Verify then `npm run generate:api` / `check:api` flow (`brief.md` Q1/Q3; P2.6-1; `recon-DEV-547:83–90`).
- **FR-008**: Web MUST show the version in exactly one Q3-selected location as `<release|dev> (<first-8-SHA-chars|unknown>)`, MUST render `"unknown"` verbatim and shorten a full SHA only in the UI, and MUST omit the line while loading or on failure (`brief.md` Q3; `ASSUMPTIONS.md`; brief decision 3).
- **FR-009**: Repository MUST include root `CHANGELOG.md` (Keep a Changelog; `Unreleased` + owner-facing `v0.1.0` covering the frozen topic list from merged behaviour) and a one-line README link. Parallel authorship is allowed; verify the entry against merged behaviour before pre-merge gates and correct drift (`brief.md` Frozen scope; Wanted 2; Done when 1; brief decision 5).
- **FR-010**: Pre-merge verification MUST cover API integration/normalization (including isolated host fixture cases and P2.6-2 override cases), OpenAPI export/Verify + `generate:api` / `check:api`, web state tests with generated MSW deterministic holds, CI/promotion/Compose parity checks, and configured C# and web gates with honest non-pass labels (`brief.md` Q5; decisions 1–3, 6–7).
- **FR-011**: Post-merge release acceptance MUST follow Q4 sequencing (DEV-546 first; tag only after both tickets on an eligible `main` commit with green nightly evidence on that exact SHA), MUST run the pre-push SHA/tag-state checks and halt/escalate on mismatch, MUST collect promote-home, GitHub Release, `RELEASE.json`, live port 3100 receipt, and Rigger YouTrack evidence before closure, and MUST distinguish readback-harness unavailability from app behaviour without dropping the port-3100 closure requirement (`brief.md` Q4; Wanted 6; Done when 4–8; brief decision 4).
- **FR-012**: Delivery MUST NOT tag GHCR images `v0.1.0`, change the gate, add release tooling/auto-bumps, or expand into excluded note types (`brief.md` Frozen scope; Out of scope).

### Key Entities

- **Version payload**: `{ release, commit }` as returned by `GET /api/version`.
- **Release manifest (`RELEASE.json`)**: Existing home promotion record; this ticket requires `"release": "v0.1.0"` (plus commit and digests) after the first tagged promotion.
- **Changelog entry (`v0.1.0`)**: Owner-facing summary of merged home-cluster behaviour listed in Wanted 2.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Isolated per-case `WebApplicationFactory` integration coverage demonstrates the version endpoint returns the configured commit and `release: null` when no release is set, plus the other FR-002/FR-003 normalization cases and the P2.6-2 override cases.
- **SC-002**: Offline deploy-parity passes, and CI `deploy-parity` remains green with Compose release empty and no runtime `LAMUFLIX_COMMIT`.
- **SC-003**: After Q3 pickup, the running app shows exactly one version line in the selected location for successful responses (including null-release `dev` with short commit and verbatim `dev (unknown)`), shows none during a deterministic in-flight hold and none on controlled failure as separate checks, and uniqueness holds across shell and status page when both exist.
- **SC-004**: `CHANGELOG.md` exists, matches merged behaviour at the pre-gate check, and the README contains one new link line to it.
- **SC-005**: When the first release is executed under Q4 / decision-4 rules on an eligible exact SHA, tag `v0.1.0`, GitHub Release, green promote-home, `RELEASE.json` `"release": "v0.1.0"`, and live port 3100 display of `v0.1.0` plus short commit are all verified, with Rigger’s ticket comment carrying the tag-run link and screenshot; harness blockers are distinguished without dropping the port-3100 receipt requirement.
- **SC-006**: Pre-merge gate and review loop complete under the Size M closing bar (Risk/Standards/Spec reports; Critical–Low in-scope findings fixed within two rounds) without lowering harness thresholds, recording actual gate verdicts including mutation FAILED when applicable.

## Assumptions

- Variable names `LAMUFLIX_COMMIT` and `LAMUFLIX_RELEASE`, `"unknown"` local commit fallback, version-line format, and omit-on-load/error behaviour are Patron `[assumed]` items recorded in `specs/DEV-547/ASSUMPTIONS.md` and frozen by brief Q2–Q3.
- DEV-546 is required before Build work (T001+ production edits and all delivery implementation), not only before work that touches the tag-run gate; changelog/README may still be authored in parallel with other delivery stories after Gate 1 and the DEV-546 prerequisite are satisfied, with a pre-gate merged-behaviour check per brief decision 5.
- Exact CHANGELOG prose for `v0.1.0` is authored in Phase B from merged behaviour using the frozen topic list — not invented in this draft.
- Display location is resolved once at Phase B pickup using the Q3 rule; this draft does not claim DEV-560 has or has not landed. Later DEV-560 landing is a drift follow-up before any placement change (brief decision 3).
- Patron P2.6-1 and P2.6-2 (and brief Plan challenge decisions 1–7) govern generated-file envelope, commit precedence, host-fixture isolation, parity/mutation preflight, deterministic smoke holds, release SHA checks, and changelog/generated-drift stops. Gate 1 remains pending.
