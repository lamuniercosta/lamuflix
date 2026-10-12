# Feature Specification: DEV-323 Movie Details view (metadata, feature-gated Play)

**Feature Branch**: `feature/323-spec`

**Created**: 2026-10-12

**Status**: Draft

**Input**: `specs/DEV-323/brief.md` (frozen 2026-10-12, Keel/Patron Q1-Q12), `specs/DEV-323/CONCLUSIONS.md`, `specs/DEV-323/ASSUMPTIONS.md`, `specs/PRODUCT.md`; `recon-DEV-323`; ticket `DEV-323:8`.

## User Scenarios & Testing

### User Story 1 - View complete movie metadata (Priority: P1)

A user opening the Movie Details view for a stored movie sees its poster, title, plot, year, runtime, directors, cast, and IMDb / Rotten Tomatoes / Metacritic badges, each rendered only when its value is present.

**Why this priority**: Ticket acceptance criterion is complete metadata on the details page; everything else builds on the loaded view.

**Independent Test**: Render `MovieDetailsView` with MSW serving a full-metadata GET response; assert title, poster alt, badges, directors, and cast. Render with partial and null metadata; assert absent fields render nothing and the no-metadata notice appears.

**Acceptance Scenarios**:

1. **Given** a movie with full metadata, **When** the details view loads, **Then** poster, title, plot, year, runtime, directors, cast, and all three badges render.
2. **Given** a movie with partial metadata, **When** the details view loads, **Then** only present fields and badges render with no "N/A" text or empty badges.
3. **Given** a movie with `metadata: null`, **When** the details view loads, **Then** title and format render plus the `No metadata available yet.` notice.
4. **Given** a missing poster, **When** the details view loads, **Then** a neutral placeholder block renders with no text and poster alt stays `<title> poster` when present.

---

### User Story 2 - Play a movie and handle the LocalPlay gate (Priority: P2)

A user in the loaded details view presses `Play` and gets inline `Playback started.` on 204, an accessible `Playback unavailable` modal on 403, an inline not-found message on 404, or an inline generic error otherwise. The button is disabled only while the request is in flight.

**Why this priority**: Ticket acceptance criterion is Play handling 204 and 403 cleanly; the server-side LocalPlay adapter stays authoritative and the UI never pre-decides playability.

**Independent Test**: Click Play under MSW 204, 403, 404, and 500 responses; assert inline success text, modal open/close/focus behavior, not-found text, and generic error text independently.

**Acceptance Scenarios**:

1. **Given** the loaded view, **When** Play returns 204, **Then** the status reads `Playback started.`
2. **Given** the loaded view, **When** Play returns 403, **Then** a dialog titled `Playback unavailable` with body `Local playback is disabled on this server.` opens, focus moves to `Close`, and Escape or Close dismisses it returning focus to Play.
3. **Given** the loaded view, **When** Play returns 404, **Then** the inline message reads `This movie is no longer in the library.`
4. **Given** the loaded view, **When** Play returns 400/422/5xx or the network fails, **Then** the inline message reads `Couldn't start playback. Try again later.`
5. **Given** a Play request in flight, **When** the user presses Play again, **Then** no second request is sent and the button reads `Starting...` until settled.

---

### User Story 3 - Reach the view and survive load failures (Priority: P3)

A user opening `/movies/<positive integer>` at initial page load sees the details view for that id with loading, not-found, and error states; any other path keeps the existing readiness view unchanged.

**Why this priority**: Q5 fallback mount without a router; entry behavior is proved without navigation framework or client-side history.

**Independent Test**: Set an explicit location path, render `App`, and assert the details view mounts with the id; restore the location and assert a non-details path renders the existing readiness screen.

**Acceptance Scenarios**:

1. **Given** initial path `/movies/42`, **When** the app loads, **Then** `MovieDetailsView` receives movie id 42.
2. **Given** initial load, **When** GET is pending, **Then** the view reads `Loading movie...`
3. **Given** GET returns 404, **When** the view settles, **Then** it reads `Movie not found.`
4. **Given** GET fails otherwise, **When** the view settles, **Then** it reads `Couldn't load this movie. Try again later.`
5. **Given** any other path, **When** the app loads, **Then** the existing readiness view renders unchanged.

### Edge Cases

- Backdrop, tagline, and writers never render, not even as placeholders (owner checkbox open; Gate 1 closed until answered).
- Keyboard focus stays inside the open 403 dialog; closing returns focus to Play.
- No hardcoded selected movie, first-movie auto-selection, browse UI, navigation framework, or client-side history.
- Tests restore any explicit location override after each case.
- MSW handlers stay scoped to these API operations; the synthetic harness handlers are not replaced globally.

## Requirements

### Functional Requirements

- **FR-001**: The view MUST fetch GET `/api/movies/{id}` and POST play through a small typed feature module consuming the delivered DEV-320 openapi-fetch client and generated contract types through TanStack Query with Zod validation, using relative `/api/...` URLs and generated MSW handlers in tests; no duplicate DTO declarations, competing client/generator, or generic client layer (Patron stack amendment, brief:143-151; constitution:313-335, constitution:470-472).
- **FR-002**: The backend MUST extend `MovieMetadataResponse` additively with nullable `posterUrl`, `rottenTomatoesRating`, `metaScore` and `directors`/`actors` string arrays (empty when none), backed by Core `MovieMetadata` and the `EfMovieCatalog.GetDetailsAsync` projection over already-stored fields; no schema or migration change.
- **FR-003**: The view MUST render each optional field and badge only when its value is present, with rating badges labelled `IMDb`, `Rotten Tomatoes`, `Metacritic`, runtime as `2h 16m` (`45m` under one hour), year as plain `YYYY`, directors/cast as comma-separated lists labelled `Director`/`Directors` and `Cast`.
- **FR-004**: The Play button MUST always render in the loaded view, stay enabled except while a request is in flight, POST to `/api/movies/{id}/play`, and map responses per US2; no feature-flag endpoint and no client-side playability decision.
- **FR-005**: The 403 modal MUST be a hand-built accessible dialog (`role="dialog"`, `aria-modal`, labelled title, focus to Close on open, Escape and Close dismiss, focus returns to Play, focus kept inside while open); native `<dialog>` `showModal` MUST NOT be used.
- **FR-006**: `App.tsx` MUST mount `MovieDetailsView` with the id parsed from `window.location.pathname` for `/movies/<positive integer>` at initial load only, keeping the readiness view elsewhere; no router dependency, no `main.tsx` rewiring beyond the existing App mount.
- **FR-007**: The contract chain MUST regenerate `web/src/api/openapi.json` and the verified snapshot through the existing `OpenApiContractTests` regeneration facility, then regenerate `web/src/api/types.ts` with delivered DEV-320 tooling; `types.ts` is generated output only, never hand-edited, and no hand-written substitute types ship while waiting for codegen.
- **FR-008**: Tests MUST cover full/partial/null metadata render, conditional badges, placeholder poster, title/format fallback, loading/404/error states, Play 204/403 (open, close, focus)/404/error, in-flight disabling, and App path-to-prop mounting with unchanged readiness outside that path, using RTL with generated contract types, Zod validation, and generated MSW handlers scoped to these API operations in fixtures.
- **FR-009**: Backend tests MUST extend real-PostgreSQL projection, endpoint mapping, and exact frozen JSON shape proofs for the new fields, null scalars, and empty collections, preserving GET 404 semantics and Playback 204/403/gate/no-process proofs; no data-driver mocks for query translation or serialization.
- **FR-010**: Scope exclusions MUST hold: no new NuGet dependency and no competing codegen npm dependency/script chosen in DEV-323 (codegen tooling stays DEV-320 owned; TanStack Query and Zod use is Patron-authorized with exact pins and consuming configuration deferred to Phase B recon, never invented here), no `package.json`/script/lockfile change for codegen in DEV-323, no `main.tsx`/`vite.config.ts`/global `index.css` edits, no backend playback/`FeatureOptions`/`Process.Start`/secrets edits, no legacy MVC `MoviesController`/`MovieService` change, no new architectural layer.

### Key Entities

- **Movie Details**: the view, query, and extended metadata; attributes: poster, plot, year, runtime, directors, cast, three rating badges, load states, Play control.
- **Playback**: the feature-gated launch flow; attributes: POST play, 204 inline success, 403 modal, 404 inline removal, generic error, in-flight guard. Server-side LocalPlay adapter is authoritative.
- **Generated contract**: the C# DTO to OpenAPI to TypeScript chain; attributes: regenerated `openapi.json` plus verified snapshot, DEV-320-generated `types.ts` consumed by the feature module.

## Success Criteria

- **SC-001**: A user with full metadata sees every named field and badge, and a user with partial or null metadata sees only present content with the specified notices and no placeholder fields.
- **SC-002**: Play reports started on 204, shows and dismisses the accessible unavailable dialog on 403 with correct focus behavior, and reports the specified inline text for 404 and all other failures without duplicate in-flight requests.
- **SC-003**: The details view mounts from the initial `/movies/<id>` path, shows loading/not-found/error states correctly, and all other paths keep the existing readiness experience.
- **SC-004**: Contract artifacts regenerate from C# with drift equality, the web compiles against generated types, and backend plus RTL/MSW suites pass with the frozen shapes updated.

## Assumptions

- Taste copy from `ASSUMPTIONS.md` (all `[assumed]`): badge labels and value formats, runtime/year formats, directors/cast labels, poster alt and placeholder, Play labels and 204 text, 403 modal title/body/button, load-state and no-metadata strings, Play 404 and generic error strings.
- Owner checkbox (CONCLUSIONS.md escalation) stays open: backdrop, tagline, and writers are out of scope with no partial rendering; Gate 1 stays closed until the owner answers. The draft proceeds on Patron's yes recommendation.
- Q5 fallback applies: no sibling owns SPA routing in these roots, so `App.tsx` mounts the view with no router (brief Q5 section).
- Q7 deferral applies: DEV-320 owns codegen tooling via owner-ticked OD-1; codegen is NOT DELIVERED at this HEAD and DEV-320 parentage under DEV-286 is NOT EVIDENCED in these roots (brief Q7 section). Build waits for delivered DEV-320 tooling before typed web implementation.
- Direct-link hosting support and any Core constructor/test caller adjustments beyond the named file list are not evidenced in these roots and need targeted Wisp recon before dependent implementation.
