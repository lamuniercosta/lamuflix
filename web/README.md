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

| Command                | What it does                                                    |
| ---------------------- | --------------------------------------------------------------- |
| `npm run dev`          | Start the Vite dev server                                       |
| `npm run lint`         | `check:api`, then ESLint (zero warnings) + `prettier --check .` |
| `npm run format`       | Rewrite files in place with Prettier                            |
| `npx tsc --noEmit`     | Typecheck `src` with no emit                                    |
| `npm test`             | Run the Vitest suite once (`vitest run`)                        |
| `npm run test:watch`   | Vitest in watch mode                                            |
| `npm run build`        | Typecheck app + tooling, then `vite build` to `dist/`           |
| `npm run generate:api` | Regenerate the typed API client and MSW handlers                |
| `npm run check:api`    | Fail if generated API files are stale (also runs in `lint`)     |

## Environment variables

- `VITE_API_BASE_URL` — optional absolute base URL for the typed API client; defaults to the browser origin.
- `VITE_ENABLE_MSW` — set to `true` in development to start the MSW browser worker; ignored outside `DEV`.

## API contract generation

`src/api/types.ts` and `src/mocks/handlers.ts` are generated from the committed
`src/api/openapi.json` and must never be hand-edited. `npm run check:api`
regenerates to a temporary file and byte-compares, so stale output fails
`npm run lint`; `npm run generate:api` rewrites both files.

When the backend contract changes, regenerate in order: export the OpenAPI
document with `LAMUFLIX_REGENERATE_OPENAPI`, then run `npm run generate:api`, and
commit `src/api/openapi.json`, `src/api/types.ts`, and `src/mocks/handlers.ts`
together.

## Gates

From the repository root, `./scripts/run-web-gates.ps1` runs the enforced web
gate suite (gated by `gates.web.enabled` in `harness.yml`). Generated output
(`node_modules/`, `dist/`, `coverage/`) is git-ignored.
