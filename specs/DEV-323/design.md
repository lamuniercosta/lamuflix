# DEV-323 — Movie Details view design

Worktree: `F:/Dev/LamuFlix.worktrees/feature-323-spec`, branch `feature/323-spec`, HEAD `30dc9e134522f3bf4df1b14c4bace6b44ca08edc`.
Sources: ticket text (DEV-323:8), recon-DEV-323, `specs/DEV-323/brief.md`, `specs/DEV-323/CONCLUSIONS.md` Q1–Q12 plus Patron stack amendment 2026-10-12 (Q6 superseded), `specs/DEV-323/ASSUMPTIONS.md`, `specs/PRODUCT.md` §5.
Facts come from recon-DEV-323. No search outside these roots.

## Owner checkbox (preserved, Gate 1 closed)

- [ ] **blocked: structural — Drop `backdrop`, `tagline` and `writers` from DEV-323 and deliver them in a follow-up ticket?**
- Patron recommends yes; the design proceeds on that basis (brief.md; CONCLUSIONS.md Q2).
- These three fields never render, not even as placeholders or skeletons. If the owner answers no, this design is re-planned with a schema/enrichment slice.

## Route map (no router)

No router dependency exists or is added: no `react-router` in `web/package.json`, runtime deps are `react` + `react-dom` only (recon-DEV-323:53,71-76). Q5 resolution (brief.md): `App.tsx` mounts the view with no new dependency.

| Path | Renders | Evidence |
|---|---|---|
| `/movies/<positive integer>` at initial page load | `MovieDetailsView movieId={id}` | brief.md Q5 fallback `[assumed]`; spec.md US3 |
| Any other path | Existing readiness view unchanged | `web/src/App.tsx:1-8`; recon-DEV-323:49-50; brief.md:28 |

Entry contract: `App` reads `window.location.pathname` once at load, parses `/movies/<positive integer>`, passes the id as a `movieId` prop. No hardcoded selected movie, no first-movie auto-selection, no browse UI, no navigation framework, no client-side history (brief.md; spec.md Edge Cases). Tests set an explicit location and restore it after each case.

Direct-link hosting (e.g. deep-link `/movies/42` served by the host on refresh) is NOT evidenced in these roots. Surface as needs recon before any deployment claim; do not assert it in implementation (brief.md Remaining state).

`main.tsx` stays the existing `App` mount (recon-DEV-323:49; brief.md).

## Layout wireframe

Loaded view (`MovieDetailsView`), text blocks:

```
+--------------------------------------------------+
| MovieDetailsView                                  |
| +----------+  Title (h1)                         |
| | poster   |  Year · Runtime (e.g. 2019 · 2h 16m) |
| | or       |  [IMDb x.x/10] [Rotten Tomatoes ..] |
| | place-   |  [Metacritic nn] (only when present) |
| | holder   |  Director(s): A, B (only when any)   |
| +----------+  Cast: C, D, E (only when any)      |
|  Plot paragraph (synopsis, only when present)     |
|  [Play]  <status region: inline success / error>  |
|  [Playback unavailable dialog, only on 403]       |
+--------------------------------------------------+
```

Non-loaded states replace the whole block:

```
Loading:        "Loading movie..."
Not found:      "Movie not found."
Other error:    "Couldn't load this movie. Try again later."
Null metadata:  Title + format + "No metadata available yet."
```

Copy is verbatim from ASSUMPTIONS.md / CONCLUSIONS.md Q10. No `N/A` text, no empty badges (Q10).

Poster: `alt="<title> poster"` when present; missing poster renders a neutral placeholder block with no text (ASSUMPTIONS.md).

Play row: button label `Play`, `Starting...` while in flight; on 204 the status reads `Playback started.` (ASSUMPTIONS.md). 404 reads `This movie is no longer in the library.` Generic error reads `Couldn't start playback. Try again later.`

## Component tree

New feature folder only; no new layer, no competing generic client/generator (brief.md Plan decisions; CONCLUSIONS.md Patron stack amendment 2026-10-12 — original Q6 superseded; PRODUCT.md §5 items 1–2):

```
App (web/src/App.tsx, edited: Q5 fallback mount)
└─ MovieDetailsView (web/src/features/movie-details/MovieDetailsView.tsx, new)
   ├─ MetadataSection (internal: poster, title, year/runtime, badges, directors/cast, plot)
   ├─ PlaySection (internal: Play button + inline status region)
   └─ PlaybackUnavailableDialog (internal hand-built dialog, rendered only on 403)
movieDetailsApi.ts (new, feature-local: details + play operations via the delivered DEV-320 openapi-fetch client and generated contract types, TanStack Query for server state, Zod validation; no native-fetch client, no hand-written DTO duplicates, no competing codegen)
MovieDetailsView.css (new, view layout/states only; no global index.css edit)
```

`App.test.tsx` gains entry-behavior tests (recognized path mounts with id; other paths keep readiness). `main.tsx`, `vite.config.ts`, global `index.css` are untouched (brief.md).

## UI states

Details load (GET-driven):

| State | Trigger | Renders |
|---|---|---|
| Loading | GET pending | `Loading movie...` |
| Not found | GET 404 | `Movie not found.` |
| Error | GET other / network failure | `Couldn't load this movie. Try again later.` |
| Loaded full | 200 + full metadata | All present fields + all three badges |
| Loaded partial | 200 + some nulls / empty arrays | Only present fields/badges; absent ones render nothing |
| Loaded null metadata | 200 + `metadata: null` | Title + format + `No metadata available yet.` |

Play (POST-driven, button always rendered and enabled except in flight; no client-side playability decision, no feature-flag endpoint — Q3/Q4):

| Response | UX |
|---|---|
| 204 | Inline `Playback started.` |
| 403 | `Playback unavailable` dialog (see Accessibility); no inline error |
| 404 | Inline `This movie is no longer in the library.` |
| 400 / 422 / 5xx / network | Inline `Couldn't start playback. Try again later.` |
| In flight | Button reads `Starting...`, disabled; second press sends no request |

Dialog dismissal: `Close` button or `Escape` closes; focus returns to Play (spec.md US2).

## Data needs (endpoint + fields)

Contract chain: C# DTO → `web/src/api/openapi.json` → generated `web/src/api/types.ts`; web consumes generated types, never hand-rolled duplicates. Codegen tooling is owned by DEV-320 OD-1 and is NOT DELIVERED at this HEAD (no `types.ts`, no generator deps/script — recon-DEV-323:78-83; brief.md Q7). DEV-323 regenerates `openapi.json` + snapshot via the existing contract facility and `types.ts` via delivered DEV-320 tooling as a forced contract-chain task; it adds no codegen dependency, pin, or competing script itself. No hand-written substitute types while waiting (brief.md). The view consumes the delivered DEV-320 openapi-fetch client and generated contract types through TanStack Query with Zod validation; tests consume generated MSW handlers from DEV-320 (brief.md; CONCLUSIONS.md Patron stack amendment — original Q6 superseded). Exact pins, delivered client/handler/provider paths, and any consuming configuration edits are deferred Phase B recon (Wisp); not invented here.

### GET Movie Details — existing contract, additive extension per Q1

- `GET /api/movies/{id:int}` → `200 MovieDetailsResponse` / `404` (recon-DEV-323:15,52: `LibraryEndpoints.cs:49-53`, `openapi.json:153-195`).
- Outer fields: `id`, `title`, `path`, `format`, `metadata` (nullable).
- Existing metadata fields (wire today): `title`, `synopsis`, `releaseYear`, `runtime`, `imdbRating`, `imdbId` (`LibraryResponses.cs:30-46`; `openapi.json:668-704`; recon-DEV-323:20).
- Additive fields this ticket projects from already-stored columns/joins (no schema change — CONCLUSIONS.md Q1; recon-DEV-323:58): nullable `posterUrl`, `rottenTomatoesRating`, `metaScore`, plus `directors: string[]` and `actors: string[]` (empty array when none).
- `backdrop`, `tagline`, `writers` are excluded by the open checkbox; they have no column/table/wire field (recon-DEV-323:18-20,58) and are not fetched or rendered.

### POST Play — existing contract, unchanged backend per Q3

- `POST /api/movies/{id}/play` → `204` / `400` / `403` / `404` / `422` (recon-DEV-323:23: `PlaybackEndpoints.cs:18-25`; `openapi.json:445-513`).
- Gate stays adapter-side, server-side: `FeatureOptions.LocalPlay` → `DisabledMediaPlayerLauncher` throws `FeatureDisabledException` → 403 via `GlobalExceptionHandler.cs:41-44` (recon-DEV-323:25). `Process.Start` lives only at `SystemProcessStarter.cs:11`.
- Request: no body. The view POSTs to the relative URL `/api/movies/{id}/play` via the feature API module consuming the delivered DEV-320 openapi-fetch client and generated contract types, TanStack Query for server state, Zod validation (CONCLUSIONS.md Patron stack amendment — original Q6 superseded). No `vite.config.ts` proxy edit; tests use generated MSW handlers from DEV-320 scoped to these operations (brief.md Test strategy). Exact pins, delivered client/handler/provider paths, and any consuming configuration edits are deferred Phase B recon (Wisp); not invented here.

## Accessibility

- 403 dialog is a hand-built accessible dialog, not a UI library and not native `<dialog> showModal` (jsdom limitation — Q9): `role="dialog"`, `aria-modal="true"`, labelled by the title `Playback unavailable`; body `Local playback is disabled on this server.`; single `Close` button.
- On open, focus moves to `Close`; focus is kept inside the dialog while open; `Escape` and `Close` dismiss; focus returns to the Play button (spec.md US2 + Edge Cases; brief.md Test strategy).
- Play button uses a real `<button>` with disabled-while-in-flight state and visible label change (`Play` / `Starting...`); inline success/error text lives in a status region announced to assistive technology.
- Poster image carries `alt="<title> poster"`; the missing-poster placeholder is a neutral block with no text (no misleading alt). Rating badges are text (`IMDb`, `Rotten Tomatoes`, `Metacritic`) and hidden entirely when null — no empty or unlabeled badges.
- Loading / not-found / error / no-metadata notices are plain text headings or status regions, keyboard-reachable by normal reading order; no interaction is trapped outside the open dialog.
- Keyboard path: Tab to Play → Enter/Space starts → on 403, Tab stays in dialog, Escape closes, focus returns to Play.
