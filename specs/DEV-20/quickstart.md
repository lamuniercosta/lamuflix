# Quickstart: DEV-20 Web Scaffold

Prerequisite: Node 24 (see `web/.node-version`). Paths below are explicit:
`<worktree>` is the named worktree root, `<worktree>/web` the scaffold,
`<worktree>/scripts` the repo gate scripts.

Local verification (run in `<worktree>/web`):

```powershell
Set-Location <worktree>/web
npm ci
npm run lint
npx tsc --noEmit
npm test
npm run test:run
npm run build
```

Type-error probe (temporary, never committed; run in `<worktree>/web`):

```powershell
Set-Location <worktree>/web
# introduce a deliberate type error in src/, run bare check (must FAIL), restore, re-run (must PASS)
npx tsc --noEmit
```

Web gate (run in `<worktree>`, not in `web/`):

```powershell
Set-Location <worktree>
pwsh -NoProfile -File ./scripts/run-web-gates.ps1
```

CI runs the frozen five-command frontend sequence from `web/`
(brief CI and gate expectations): `npm ci`, `npm run lint`,
`npx tsc --noEmit`, `npm test`, `npm run build`.
`npm run test:run` is an additional local/Gauge receipt alongside the CI
sequence, not a sixth CI step.
