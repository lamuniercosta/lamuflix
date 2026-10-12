# Research: DEV-323 Movie Details view

**Date**: 2026-10-12 | **Sources**: `brief.md`, `recon-DEV-323`, `specs/PRODUCT.md`. No searches outside the authorized roots.

## Backend surface (exists; extend additively)

- GET `/movies/{id:int}` endpoint, query/handler, port, and `MovieDetails` record exist (recon-DEV-323:15-17). Core `MovieMetadata` and the EF projection plus `MovieMetadataResponse` carry only Title, Synopsis, ReleaseYear, Runtime, ImdbRating, ImdbId (recon-DEV-323:18-20).
- POST `/movies/{id}/play` exists with 204/400/403/404/422; the LocalPlay gate is adapter-side with `DisabledMediaPlayerLauncher` throwing `FeatureDisabledException`, mapped to 403 (recon-DEV-323:23-25). Playback backend stays untouched.

## Storage (no schema change)

- Poster, RT rating, meta score, plot, and movies-to-actors/directors/genres joins are already stored; no backdrop or tagline column and no Writer table exist (recon-DEV-323:58). Backdrop/tagline/writers stay out under the open owner checkbox.

## Frontend base (scaffold only)

- `web/src` holds 8 files: `App.tsx` readiness component, `main.tsx` mount, `App.test.tsx` (2 tests), `index.css`, committed `api/openapi.json`, MSW synthetic harness (recon-DEV-323:48-51). No router, API client, fetch in app code, or state library; no `types.ts`; no codegen script or generator dependency (recon-DEV-323:53,83).

## Decisions carried into plan

- Q5 fallback: no sibling owns SPA routing in these roots, so `App.tsx` mounts `MovieDetailsView` at the initial `/movies/<positive integer>` path with no router (brief Q5 section; recon-DEV-323:71-76).
- Q7 deferral: DEV-320 owns codegen tooling via OD-1; codegen is NOT DELIVERED at this HEAD and DEV-320 parentage is NOT EVIDENCED in these roots (brief Q7 section; recon-DEV-323:78-83). Typed web implementation waits for delivered DEV-320 tooling.
- Patron stack: TanStack Query, Zod, delivered DEV-320 openapi-fetch client, and generated MSW handlers (brief:143-151; constitution:313-335, constitution:470-472). No native-fetch/no-query instruction remains. Exact pins and delivered client/handler paths are Phase B recon, never invented here.
- Contract regeneration uses the existing `OpenApiContractTests` facility for `openapi.json` plus the verified snapshot; frozen-shape endpoint/e2e tests extend for the additive fields (recon-DEV-323:32-34).
- Open recon needs (not decided here): delivered DEV-320 codegen evidence at build pickup, direct-link hosting support, and any Core constructor/test caller paths beyond the named list (brief remaining-state section).
