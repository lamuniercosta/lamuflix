# DEV-501 — Phase B pickup receipt (T002–T004)

Read-only pickup verification for `DEV-501` on branch
`feature/DEV-501-search-and-download-subtitles-by-language-from-the-player`.
Recorded by Anvil at Phase 2 kickoff. Facts only; no product decision is made here.

## T001 — Gate 1 recorded

- `gh pr view 381 --json state,mergedAt,title` →
  `{"state":"MERGED","mergedAt":"2026-11-12T08:13:09Z","title":"DEV-501: spec"}`.
- `specs/DEV-501/spec.md:5` carries `gate1: provisional` with zero owner structural checkboxes;
  `specs/DEV-501/CONCLUSIONS.md` "Phase 2 Step 7 — `gate1: provisional` Review" records the ruling.
- Verdict: Gate 1 approval is recorded. Phase 2 may start.

## T002 — feature pointer and branch

- `.specify/feature.json` is **absent** in this worktree (`.specify/` holds
  `integrations memory scripts templates workflows` only; `git ls-files | Select-String feature.json`
  returns no path). The spec-PR branch `feature/501-spec` owns the Spec Kit checkout; this build
  branch does not carry the pointer file.
- `git branch --show-current` →
  `feature/DEV-501-search-and-download-subtitles-by-language-from-the-player`.
- `specs/DEV-501/tasks.md` is present in this worktree, so the feature directory resolves by path.
- Verdict: pointer file absent (expected on the build branch, not drift); feature directory
  `specs/DEV-501` is the active one. No amendment needed.

## T003 — dependency readiness and current paths on current main

- `git log --oneline -1 origin/main` → `bac075b0 DEV-483: spec`.
- `git diff --stat origin/main -- specs/DEV-501 docs/adr/0041-explicit-subtitle-acquisition.md`
  → only `docs/adr/0041-explicit-subtitle-acquisition.md` (3 insertions, 3 deletions: the
  ticket-authorized `proposed` → `accepted` transition). `specs/DEV-501` shows **zero drift**.
- Current existing paths verified in this checkout (recon:113–130 inventories confirmed rather than
  assumed):
  - `src/LamuFlix.Api/Endpoints/SubtitleEndpoints.cs` — list/content/prepare/sidecar routes present;
    prepare mapping at lines 57–66.
  - `src/LamuFlix.Api/HandlerRegistration.cs:86-101` — subtitle handlers and acquisition options
    registered, including `PrepareSubtitleCommandHandler` at line 88.
  - `src/LamuFlix.Core/Features/Subtitles/` — `PrepareSubtitleCommandHandler.cs`,
    `SubtitleSidecarDiscovery.cs`, `SubtitleAcquisitionSettings.cs`, `SubtitleLookup.cs`,
    `MovieHashReader.cs`, `SubtitleTrackToken.cs`, `SubtitleTelemetry.cs`, `SubtitleDiagnostics.cs`
    all present.
  - `src/LamuFlix.Core/Options/SubtitlesOptions.cs`, `SubtitlesOptionsValidator.cs`,
    `SubtitleProviderOptions.cs`, `SubtitleProviderOptionsValidator.cs` present.
  - `src/LamuFlix.Core/Ports/ISubtitleProvider.cs`, `ISubtitleSidecarStore.cs`,
    `SubtitleCandidate.cs`, `SubtitleSearch.cs` present.
  - `src/LamuFlix.Infrastructure/Adapters/OpenSubtitlesSubtitleProvider.cs`,
    `SubtitlePayloadValidator.cs`, `SubtitleProviderServiceCollectionExtensions.cs` present.
  - `src/LamuFlix.Infrastructure/FileSystem/SubtitleSidecarStore.cs` present.
  - Tests: `tests/LamuFlix.UnitTests/Features/Subtitles/` (PrepareSubtitleCommandHandlerTests,
    SubtitleSidecarDiscoveryTests, SubtitleLookupTests, MovieHashPropertyTests, …),
    `tests/LamuFlix.UnitTests/FileSystem/SubtitleSidecarStoreTests.cs`,
    `tests/LamuFlix.IntegrationTests/OpenSubtitlesSubtitleProviderTests.cs`,
    `SubtitleSidecarStoreConcurrencyTests.cs`, `SubtitlePrepareServeEndpointTests.cs`,
    `SubtitleEndpointTests.cs`, `OpenApiContractTests.cs`.
- Player CC panel and Subs discovery dependencies are satisfied by the paths above:
  `SubtitleSelector`-side playback files and `Subs\` enumeration in
  `LibraryFileReader.EnumerateRegularFiles`/`SubtitleSidecarDiscovery` already exist on `main`.
- Verdict: dependencies ready; no task amendment required; no `needs recon` item.

## T004 — read-only drift analysis of the approved artifacts

- `/speckit-analyze` prerequisite script
  (`.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks`) exits **1**
  with `Not on a feature branch. Current branch: feature/DEV-501-…` because that script only accepts
  `001-…`/`1234-…`/timestamp branch names. The analysis was therefore run read-only over the same
  three artifacts directly.
- Cross-artifact result against `origin/main`:
  - `spec.md` FR-001…FR-023 and SC-001…SC-008 map onto `tasks.md` through the committed coverage
    table at `tasks.md:18-52`; no requirement lacks a task.
  - `plan.md` exact file surface matches the paths verified in T003; the only artifact that moved is
    ADR 0041's status line, which T005 consumes deliberately.
  - Constitution Principle VII still names the prepare route, which T005 amends; no other principle
    in the current text conflicts with the approved plan.
  - No contradiction with ticket scope, no new dependency/layer/schema, `Features:LocalPlay` untouched.
- Verdict: no drift requiring a task amendment. T005–T010 proceed as written.

## Authorized amendments

None. Task text is unchanged from the frozen `tasks.md`.
