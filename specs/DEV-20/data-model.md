# Data Model: DEV-20 Web Scaffold

No domain data, entities, migrations, or storage. This scaffold introduces no persistent model.

## Static structures (config-level only)

- **Package manifest** (`web/package.json`): 24 frozen direct packages, exact versions; scripts `dev`, `build`, `lint`, `format`, `test`, `test:run`, `test:watch`; `engines.node >=24 <25`.
- **TS configs**: root `tsconfig.json` (`strict: true`, directly includes `src` app + tests); `tsconfig.node.json` covers `vite.config.ts`.
- **Test harness**: `src/test/setup.ts` (RTL cleanup, jest-dom), `src/test/server.ts` (MSW node server, `onUnhandledRequest: error`), synthetic handlers only.

No relationships, state transitions, or validation rules beyond the build/test contracts in `spec.md` FR-004-FR-007.
