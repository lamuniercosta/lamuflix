# MSW Harness Contract (test-only, no product API)

No product routes, DTOs, or status codes are introduced (spec FR-011). This contract governs the synthetic test harness only.

- Server: `src/test/server.ts` MSW node server, `listen({ onUnhandledFrame: 'error' })` in `beforeAll`, `resetHandlers()` in `afterEach`, `close()` in `afterAll`.
- Handlers: synthetic non-product URLs only (e.g. `https://test.local/*`); one default handler plus per-test overrides proving reset/isolation.
- App: `src/App.tsx` performs no fetch; screen tests run independently of MSW.
