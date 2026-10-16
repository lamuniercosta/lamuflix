# DEV-432 Implementation Quickstart

## Preconditions

- Human Gate 1 has approved `spec.md`, `plan.md`, and `tasks.md`.
- Work in `F:/Dev/LamuFlix.worktrees/feature-DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba`.
- The frontend-only boundary is confirmed. Details displays available fields, preserves Play's current 403 behavior, and defers genres/proactive playback capability to a backend follow-up. Do not change `web/src/api/**`, backend files, or generated files.
- Dependencies and test framework remain as currently installed in `web/package.json`.

## Spec-kit prerequisite selection

The default `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` rejects the actual assigned Git branch name and exits 1 because its branch-name validator accepts numeric/timestamp feature names. The feature directory remains pinned by `.specify/feature.json`. To select that pinned feature directory without renaming the Git branch, set the supported logical feature selector for this process only, run the same prerequisite command, and restore the prior environment value:

```powershell
$hadSpecifyFeature = Test-Path Env:SPECIFY_FEATURE
$previousSpecifyFeature = $env:SPECIFY_FEATURE
try {
  $env:SPECIFY_FEATURE = '432-app-layout'
  pwsh -NoProfile -File ./.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks
  $prerequisiteExit = $LASTEXITCODE
}
finally {
  if ($hadSpecifyFeature) { $env:SPECIFY_FEATURE = $previousSpecifyFeature }
  else { Remove-Item Env:SPECIFY_FEATURE -ErrorAction SilentlyContinue }
}
if ($prerequisiteExit -ne 0) { exit $prerequisiteExit }
```

For this planning set, the selector-specific invocation returned exit 0 with `FEATURE_DIR` exactly `F:/Dev/LamuFlix.worktrees/feature-DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba/specs/DEV-432` and `AVAILABLE_DOCS` containing `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, and `tasks.md`. The actual Git branch remained `feature/DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba`. This does not validate or change that branch's naming pattern; the default invocation's exit 1 remains a separate limitation.

## Local implementation checks

From the repository root, run the web gate after implementation:

```powershell
pwsh -NoProfile -File ./scripts/run-web-gates.ps1 -All
```

This gate runs lint, TypeScript checking, Vitest, and the production build. Record the actual command output and exit code; only exit 0 is PASS. An exit 2 is SKIPPED, not PASS.

Use existing focused tests while developing:

```powershell
Set-Location web
npx vitest run src/App.test.tsx
npx vitest run src/library
npx vitest run src/features/library
npx vitest run src/features/movie-details
```

Adjust test path arguments to the test files added or changed; do not add another test runner.

## Manual acceptance evidence

At 375px, 768px, and 1440px, capture the Library, Watchlist, and Details views. Also check the exact 767px/768px transition. Use keyboard-only input for route navigation, filter operation, drawer open/close, focus containment/return, and body-scroll release. Exercise query chips including one-sided ranges, query preservation, page reset, Back/Forward, direct route/reload, details search without Enter and with Enter, import, bookmark optimistic rollback, enrichment retry, missing poster/metadata, and Play 403/404/failure.

Check actual foreground/background pairs: normal text 4.5:1, large text 3:1, and meaningful control boundaries/focus indicators 3:1. Save measured results and screenshots under `artifacts/DEV-432/screenshots/`; save concise gate output under `artifacts/DEV-432/web-gates/`; write results to `artifacts/DEV-432/acceptance.md`.

Capture the README library screenshot from a stable populated view showing the shell, filters, active chips, result count, sort, and poster grid. The screenshot and manual evidence are future implementation checks; this planning document records no completed test or visual review.
