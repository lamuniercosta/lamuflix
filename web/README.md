# LamuFlix web

Vite + React + TypeScript frontend for LamuFlix.

## Requirements

- **Node 24** — pinned by `.node-version` and `engines.node` (`>=24 <25`); install with `nvm use`, `fnm use`, or any tool that reads `.node-version`.
- **npm** — unpinned; any npm shipped with Node 24 works.

## Setup

Install dependencies from the committed lockfile (never `npm install` by hand):

```bash
cd web
npm ci
```

## Commands

All commands run from `web/`:

| Command              | What it does                                          |
| -------------------- | ----------------------------------------------------- |
| `npm run dev`        | Start the Vite dev server                             |
| `npm run lint`       | ESLint (zero warnings) + `prettier --check .`         |
| `npm run format`     | Rewrite files in place with Prettier                  |
| `npx tsc --noEmit`   | Typecheck `src` with no emit                          |
| `npm test`           | Run the Vitest suite once (`vitest run`)              |
| `npm run test:watch` | Vitest in watch mode                                  |
| `npm run build`      | Typecheck app + tooling, then `vite build` to `dist/` |

## Gates

From the repository root, `./scripts/run-web-gates.ps1` runs the enforced web
gate suite (gated by `gates.web.enabled` in `harness.yml`). Generated output
(`node_modules/`, `dist/`, `coverage/`) is git-ignored.
