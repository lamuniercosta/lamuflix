# DEV-570 T010 — Integrated Chromium receipt (local Build)

- Executed 2026-11-26T01:54Z via the connected **Test URLs** portal in Chromium against the local Compose stack at `http://localhost:3000` (Rigger receipt: OK stack at `e6240cc3`). No other URL, external browser, or hosted deployment was used. No media was played; portal media pause check returned `true`; no Web Audio context was created.
- Local Build only: no Cog refactor, Gauge gate, review, or hosted-merge-readiness claim is made here.

## Seed precondition (recorded, not silently assumed)

At pickup the running stack served **0 imported movies** (`GET /api/movies?page=1&pageSize=100&sort=Title&direction=Ascending` → `{"items":[],"totalCount":0}`; `/movies/512` → "Movie not found."). The real library folders were visible to `GET /api/movies/import/candidates`, so the enriched-movie precondition was established through the app itself in the portal: the Bulk Import dialog imported `007 James Bond Collection/Casino Royale[2006]`, and the same dialog endpoint (`POST /api/movies/import/library`) imported the additional real folders used below. OMDb enrichment completed normally. The null-metadata movie was imported as a real library folder and then given a deliberately nonexistent IMDb ID (`tt99999999`) via the app's existing manual-match endpoint, so it remains stably null-metadata (`enrichmentStatus: Failed`, `failureCategory: invalid_response`, `metadata: null`).

- Enriched/mapped-cast movie: **id 6 — Justice League Dark Apokolips War**, file `Justice League Collection/Justice League Dark Apokolips War[2020]`; metadata directors `[Matt Peters, Christina Sotta]`, actors `[Roger Cross, Rosario Dawson, Christopher Gorham, Camilla Luddington]`, 26 writers, genres `[Action, Animation, Sci-Fi]`, `collectionName: null` (the TMDB collection supplement is disabled on this stack, so the Collection row is correctly absent under the "Collection when present" rule).
- Null-metadata movie: **id 11 — You Can See Everything**, file `You Can See Everything[2026]`.

## Same-movie screenshot pair (FR-006, FR-010, SC-005)

Same movie and route for both captures: `http://localhost:3000/movies/6`.

| Capture | Portal viewport | Page metrics | Evidence |
|---|---|---|---|
| Windowed | 1280×800 (`window.innerWidth` 1280, `innerHeight` 792) | About heading scrolled to y≈86; about list 1185 px wide; all rows visible | `T010-about-windowed-1280x800.png` |
| Phone width | 390×844 (`window.innerWidth` 390, `innerHeight` 844) | `scrollY` 1257; About heading at top; values wrap | `T010-about-phone-390x844.png` |

Phone-width wrapping measured on the same route: Directors 1 rendered line, Cast 3 lines (24 px line-height × 3 minus chip padding, 62 px total), Writers 22 lines (528 px / 24 px), Genres 1 line. Computed value styling on the pair: `overflow-wrap: anywhere`, `white-space: normal`; grey label `dt` colour `rgb(164, 156, 174)`, white value `dd` colour `rgb(244, 239, 230)` — existing tokens, no new styling check claimed beyond visual inspection.

## SMOKE-001 — eligible About rows in order

- **Route / viewport**: `http://localhost:3000/movies/6`, 1280×800 (windowed).
- **Result: PASS.** Heading `About Justice League Dark Apokolips War` with one heading-labelled `dl`; rows in ticket order: `Directors` → `Matt Peters, Christina Sotta`; `Cast` → `Roger Cross, Rosario Dawson, Christopher Gorham, Camilla Luddington`; `Writers` → the 26 stored writer strings, comma-separated in stored order (`Mairghread Scott (story by), Ernie Altbacker (teleplay by), … Murphy Anderson (created by: Zatanna)`); `Genres` → `Action, Animation, Sci-Fi`. Values match `GET /api/movies/6`. Collection is absent because `collectionName` is null on this stack, which is the specified behavior for a missing Collection value.

## SMOKE-002 — mapped About cast opens existing More-with panel

- **Route / viewport**: `http://localhost:3000/movies/6`, 1280×800 (windowed).
- **Result: PASS.** All four About Cast names render as mapped selection buttons. Activating `Roger Cross` opened the existing panel headed `More with Roger Cross in your library`, with `See all with Roger Cross in Library` (`/?actorIds=16`) and `No other movies with Roger Cross in your library yet.` The same `aria-pressed="true"` state appears on the hero and About chips, i.e. the existing page selection path is reused; the hero cast section still lists the same four names and remains present.
- **Evidence**: `T010-smoke-002-more-with-1280x800.png`.

## SMOKE-003 — separate null-metadata movie

- **Route / viewport**: `http://localhost:3000/movies/11`, 1280×800 (windowed); deliberately distinct from the screenshot movie.
- **Result: PASS.** No `About` heading and no `dl.details-about-list` in the DOM. The existing notice `No metadata available yet.` remains, together with `The last metadata lookup failed.` and the existing `Fetch metadata` / `Match manually` actions. `GET /api/movies/11` reports `metadata: null`.
- **Evidence**: `T010-smoke-003-null-metadata-1280x800.png`.

## Files preserved

- `specs/DEV-570/evidence/T010-about-windowed-1280x800.png`
- `specs/DEV-570/evidence/T010-about-phone-390x844.png`
- `specs/DEV-570/evidence/T010-smoke-002-more-with-1280x800.png`
- `specs/DEV-570/evidence/T010-smoke-003-null-metadata-1280x800.png`
- `specs/DEV-570/evidence/T010-smoke-notes.md` (this file)
