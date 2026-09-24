# 0013. Solution layout: src/ and tests/

- Status: Accepted
- Date: 2026-09-23
- Ticket: DEV-290 (parent DEV-281)

## Context

The solution kept all four projects in flat root folders: `LamuFlix.Web/`, `LamuFlix.Data/`,
`LamuFlix.Work/`, and `LamuFlix.Test/`. The folder `LamuFlix.Work/` did not match its project
and namespaces, which were already `LamuFlix.Worker`. The root also held two Visual Studio
installer projects (`SetupWorker/`, `LamuFlix.WorkerSetup/`) that the solution no longer builds.
The architecture plan, §3 *Target Solution Layout*, puts production projects under `src/` and
test projects under `tests/`. §11, Epic 1 item 2, schedules this move. The constitution names
the flat layout as Known Technical Debt ("Transitional layout").

## Decision

- Move the production projects to `src/LamuFlix.Web/`, `src/LamuFlix.Data/`, and
  `src/LamuFlix.Worker/`. The last move also corrects the stale folder name.
- Move the test project to `tests/LamuFlix.Test/`.
- Change only the path strings in `LamuFlix.sln` and in the `ProjectReference` items.
- Keep every project name and namespace. Web is not renamed to Api, and Data is not renamed to
  Infrastructure. Those renames belong to later Epic 1 work.
- Delete the installer projects and the unreferenced `LamuFlix.Data/Models/Temp.cs`.

## Consequences

- New projects go under `src/` or `tests/`.
- Each move is a pure `git mv`, so `git log --follow` and blame keep file history.
- The constitution's "Transitional layout" debt entry and its deferred TODO still describe the
  old folders. A follow-up ticket updates them.
- The later Epic 1 renames (Api, Infrastructure, Core) start from this layout.
