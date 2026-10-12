# Quickstart: DEV-323 Movie Details view

**Date**: 2026-10-12

1. Confirm the owner checkbox answer on the spec PR before Gate 1 (backdrop/tagline/writers stay out until checked).
2. At Phase B pickup, verify delivered DEV-320 codegen tooling exists in the build base; stop dependent web work if absent (needs recon).
3. Implement additive Core metadata plus the EF projection, then API mapping with frozen-shape tests (real PostgreSQL; no data-driver mocks).
4. Regenerate `web/src/api/openapi.json` plus the verified snapshot through the existing contract facility and prove drift equality; generate `web/src/api/types.ts` with DEV-320 tooling.
5. Build the feature-local view, API module (DEV-320 openapi-fetch client with TanStack Query and Zod), styles, and RTL tests with generated MSW handlers using web-implement TDD; mount through `App.tsx` and prove entry behavior.
6. Keep implementation TDD/contract checks; final gates are Gauge-owned after Cog refactor. Unaccepted property-test exit-2 remains blocking; report SKIPPED/SKIP/NOT APPLICABLE honestly, never as PASS.
