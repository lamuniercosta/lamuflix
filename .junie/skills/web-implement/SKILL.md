---
name: web-implement
description: Implement frontend features and components in web/ using a strict TDD loop (Vitest + React Testing Library) driven by tasks.md, followed by web gate verification. Used by the Builder (Anvil).
---

# Web Implementation Skill (`web-implement`)

## 1. Overview

This skill guides component and UI development within `web/` using test-driven development (TDD) with Vitest and React Testing Library (RTL). All work is derived from `specs/<feature>/tasks.md` and `specs/<feature>/design.md` (when present).

## 2. Prerequisites

- Check out the task branch or active worktree.
- Read `specs/<feature>/tasks.md` and any UI contract in `specs/<feature>/design.md`.
- Inspect API contracts in `web/src/api/` and `web/src/api/types.ts`.

## 3. The TDD Loop (Red -> Green -> Refactor)

For each task in `tasks.md`:

1. **Red (Failing Test)**:
   - Write unit/component tests in `web/src/.../*.test.tsx` or `web/src/.../*.spec.ts`.
   - Use React Testing Library queries (`screen.getByRole`, `screen.getByLabelText`) and userEvent.
   - Run tests:
     ```bash
     cd web && npm run test:run -- <TestFile>
     ```
   - Confirm test fails for the expected reason.

2. **Green (Minimal Implementation)**:
   - Implement the component, hook, or utility to satisfy the test.
   - Run tests again until they pass.

3. **Refactor**:
   - Clean up code, adhere to `react-best-practices` (eliminate waterfalls, avoid unnecessary re-renders).
   - Ensure clean Tailwind/CSS styling adhering to `specs/DESIGN.md`.

## 4. Web Gates Verification

Before marking any task complete or reporting back to Conductor/Keel, run the web gate script from repository root:

```powershell
./scripts/run-web-gates.ps1
```

If `run-web-gates.ps1` is not yet available in the early reshuffle phase, execute the mechanical suite manually:
```bash
cd web
npm run lint
npm run build
npm run test:run
```

Ensure:
- TypeScript compiles with zero errors (`tsc --noEmit`).
- ESLint passes cleanly.
- All Vitest suites pass.
- Vite build succeeds.

## 5. Report-Back Contract

Report back with:
- Tasks completed from `tasks.md`.
- Verification command output summary (all gates passing).
- Staged file list.
